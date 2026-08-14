using System.Text.Json;
using AdaptiveTeamBuilder.Data.Contracts;
using Microsoft.Extensions.Options;

namespace AdaptiveTeamBuilderSvc;

/// <summary>
/// Trivial per-dimension observation counter (occurrences for, occurrences against,
/// first-seen date), computed in code alongside the agents.
/// Never wired to the UI or to the prompts — it exists solely so the harness can display
/// "the agent moved at 4 observations here and 19 there" as a fact rather than an impression.
/// </summary>
public sealed class ShadowCounterService(
    IOptions<AgentFrameworkOptions> options,
    IHostEnvironment environment,
    ILogger<ShadowCounterService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// Folds this batch's user-caused interactions into the per-user counters and returns
    /// the updated snapshot for inclusion in the run record.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, ShadowCounterSnapshot>> UpdateAsync(
        Guid userId,
        IReadOnlyList<InteractionDto> interactions,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var counters = await ReadAsync(userId, cancellationToken);
            var now = DateTime.UtcNow.ToString("yyyy-MM-dd");

            foreach (var interaction in interactions)
            {
                // Only user-caused acts are evidence; system-produced states never count.
                if (!string.Equals(interaction.Causation, "user", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                switch (interaction.Action)
                {
                    case "view.change"
                        when string.Equals(
                            interaction.Meta?.GetValueOrDefault("preferenceAxis"),
                            "signalsDisplay",
                            StringComparison.OrdinalIgnoreCase):
                    {
                        var to = interaction.Meta?.GetValueOrDefault("to");
                        // "for" counts observations toward charted; "against" toward bare.
                        Bump(
                            counters,
                            "information-form",
                            string.Equals(to, "graph", StringComparison.OrdinalIgnoreCase),
                            now);
                        break;
                    }

                    case "control.expand":
                        Bump(counters, "disclosure-default", forObservation: true, now);
                        if (interaction.ChoiceSet is { Count: > 1 })
                        {
                            Bump(counters, "selection-rule", forObservation: true, now);
                        }

                        break;

                    case "control.collapse":
                        Bump(counters, "disclosure-default", forObservation: false, now);
                        break;

                    case "signal.focus":
                    case "signal.activate":
                        Bump(counters, "metric-attention", forObservation: true, now);
                        break;
                }
            }

            await WriteAsync(userId, counters, cancellationToken);
            return counters;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyDictionary<string, ShadowCounterSnapshot>> SnapshotAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await ReadAsync(userId, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static void Bump(
        Dictionary<string, ShadowCounterSnapshot> counters,
        string dimension,
        bool forObservation,
        string now)
    {
        if (!counters.TryGetValue(dimension, out var counter))
        {
            counter = new ShadowCounterSnapshot { FirstSeen = now };
            counters[dimension] = counter;
        }

        counter.FirstSeen ??= now;
        if (forObservation)
        {
            counter.For++;
        }
        else
        {
            counter.Against++;
        }
    }

    private async Task<Dictionary<string, ShadowCounterSnapshot>> ReadAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var path = ResolvePath(userId);
        if (!File.Exists(path))
        {
            return [];
        }

        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<Dictionary<string, ShadowCounterSnapshot>>(
                stream, JsonOptions, cancellationToken) ?? [];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to read shadow counters for {UserId}.", userId);
            return [];
        }
    }

    private async Task WriteAsync(
        Guid userId,
        Dictionary<string, ShadowCounterSnapshot> counters,
        CancellationToken cancellationToken)
    {
        try
        {
            var path = ResolvePath(userId);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(
                path,
                JsonSerializer.Serialize(counters, JsonOptions),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to write shadow counters for {UserId}.", userId);
        }
    }

    private string ResolvePath(Guid userId) =>
        Path.Combine(
            PathUtilities.Resolve(environment, options.Value.DataDirectory),
            "shadow-counters",
            $"{userId:D}.json");
}
