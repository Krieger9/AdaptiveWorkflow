using System.Text;
using System.Text.Json;
using AdaptiveTeamBuilder.Data.Contracts;
using Microsoft.Extensions.Options;

namespace AdaptiveTeamBuilderSvc;

public interface IInteractionLog
{
    /// <summary>Appends interactions to the per-user/session JSONL log. Never mutates.</summary>
    Task AppendAsync(
        Guid userId,
        string sessionId,
        IReadOnlyList<InteractionDto> interactions,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<InteractionDto>> ReadSessionAsync(
        Guid userId,
        string sessionId,
        CancellationToken cancellationToken);

    IReadOnlyList<string> ListSessions(Guid userId);
}

/// <summary>
/// Append-only JSONL per session: data/interactions/{userId}/{sessionId}.jsonl.
/// Replay depends on this being never mutated.
/// </summary>
public sealed class FileInteractionLog(
    IOptions<AgentFrameworkOptions> options,
    IHostEnvironment environment,
    ILogger<FileInteractionLog> logger) : IInteractionLog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task AppendAsync(
        Guid userId,
        string sessionId,
        IReadOnlyList<InteractionDto> interactions,
        CancellationToken cancellationToken)
    {
        if (interactions.Count == 0)
        {
            return;
        }

        try
        {
            var path = ResolvePath(userId, sessionId);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var sb = new StringBuilder();
            foreach (var interaction in interactions)
            {
                sb.AppendLine(JsonSerializer.Serialize(interaction, JsonOptions));
            }

            await _gate.WaitAsync(cancellationToken);
            try
            {
                await File.AppendAllTextAsync(path, sb.ToString(), cancellationToken);
            }
            finally
            {
                _gate.Release();
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to append to interaction log for session {SessionId}.", sessionId);
        }
    }

    public async Task<IReadOnlyList<InteractionDto>> ReadSessionAsync(
        Guid userId,
        string sessionId,
        CancellationToken cancellationToken)
    {
        var path = ResolvePath(userId, sessionId);
        if (!File.Exists(path))
        {
            return [];
        }

        var interactions = new List<InteractionDto>();
        foreach (var line in await File.ReadAllLinesAsync(path, cancellationToken))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var interaction = JsonSerializer.Deserialize<InteractionDto>(line, JsonOptions);
                if (interaction is not null)
                {
                    interactions.Add(interaction);
                }
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Skipping malformed interaction log line in {SessionId}.", sessionId);
            }
        }

        return interactions;
    }

    public IReadOnlyList<string> ListSessions(Guid userId)
    {
        var directory = Path.Combine(DataRoot(), "interactions", userId.ToString("D"));
        if (!Directory.Exists(directory))
        {
            return [];
        }

        return Directory.EnumerateFiles(directory, "*.jsonl")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
    }

    private string DataRoot() => PathUtilities.Resolve(environment, options.Value.DataDirectory);

    private string ResolvePath(Guid userId, string sessionId)
    {
        var safeSession = SanitizeToken(sessionId);
        return Path.Combine(
            DataRoot(),
            "interactions",
            userId.ToString("D"),
            $"{safeSession}.jsonl");
    }

    internal static string SanitizeToken(string value)
    {
        var chars = value.Trim()
            .Select(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_' ? ch : '-')
            .ToArray();
        var token = new string(chars).Trim('-');
        return string.IsNullOrEmpty(token) ? "unknown" : token;
    }
}
