using System.Diagnostics;
using System.Threading.Channels;
using AdaptiveTeamBuilder.Data.Contracts;

namespace AdaptiveTeamBuilderSvc;

public record CollaborationProfileUpdateContext(
    string? SurfacePath,
    string? SurfaceTitle,
    ViewStateDto? ViewState,
    IReadOnlyDictionary<string, string>? SurfaceAnnotations,
    int? VisibleControlCount = null,
    IReadOnlyList<ControlSnapshotDto>? Controls = null,
    IReadOnlyList<string>? RecentTurnDigests = null,
    string? AssembledContext = null,
    string? ContextHash = null,
    string? SessionId = null,
    string Trigger = "flush-on-action",
    string? PromptOverride = null);

public record CollaborationProfileUpdateWorkItem(
    Guid UserId,
    IReadOnlyList<InteractionDto> Events,
    CollaborationProfileUpdateContext? Context = null);

public interface ICollaborationProfileUpdateQueue
{
    ValueTask EnqueueAsync(
        CollaborationProfileUpdateWorkItem item,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<CollaborationProfileUpdateWorkItem> ReadAllAsync(
        CancellationToken cancellationToken);
}

public sealed class CollaborationProfileUpdateQueue : ICollaborationProfileUpdateQueue
{
    private readonly Channel<CollaborationProfileUpdateWorkItem> _channel =
        Channel.CreateUnbounded<CollaborationProfileUpdateWorkItem>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
            });

    public ValueTask EnqueueAsync(
        CollaborationProfileUpdateWorkItem item,
        CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(item, cancellationToken);

    public IAsyncEnumerable<CollaborationProfileUpdateWorkItem> ReadAllAsync(
        CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}

public sealed class CollaborationProfileUpdateBackgroundService(
    ICollaborationProfileUpdateQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<CollaborationProfileUpdateBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessAsync(item, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to update belief profile for user {UserId} from {EventCount} interaction(s).",
                    item.UserId,
                    item.Events.Count);
            }
        }
    }

    internal async Task<AgentRunRecord> ProcessAsync(
        CollaborationProfileUpdateWorkItem item,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IBeliefProfileStore>();
        var updater = scope.ServiceProvider.GetRequiredService<ICollaborationProfileUpdater>();
        var shadowCounters = scope.ServiceProvider.GetRequiredService<ShadowCounterService>();
        var runRecorder = scope.ServiceProvider.GetRequiredService<IAgentRunRecorder>();
        var glossary = scope.ServiceProvider.GetRequiredService<GlossaryProvider>();

        var stopwatch = Stopwatch.StartNew();
        var current = await store.GetAsync(item.UserId, cancellationToken);
        var context = item.Context is null
            ? new CollaborationProfileUpdateContext(
                null,
                null,
                null,
                null,
                RecentTurnDigests: current.RecentTurnDigests)
            : item.Context with { RecentTurnDigests = current.RecentTurnDigests };

        var events = CollaborationContextFormatter.FlagReversals(item.Events);
        var outcome = await updater.UpdateFromObservationsAsync(
            current,
            events,
            context,
            cancellationToken);

        var surfacePath = context.SurfacePath ?? BeliefDocumentFormat.ContractsListScope;
        var digest = CollaborationContextFormatter.FormatDecisionTurnDigest(
            context.Controls,
            context.ViewState,
            events,
            CollaborationContextFormatter.ActiveProfileSummary(current),
            surfacePath);

        int? versionOut = null;
        string? profileDiff = null;
        var documentChanged = !string.Equals(
            outcome.Profile.Document,
            current.Document,
            StringComparison.Ordinal);
        if (documentChanged && outcome.ValidationResult != "rejected")
        {
            versionOut = await store.SaveAsync(
                item.UserId,
                outcome.Profile.Document,
                outcome.Profile.Source,
                cancellationToken);
            profileDiff = BeliefDocumentFormat.UnifiedDiff(
                current.Document,
                outcome.Profile.Document);
        }

        var digestId = await store.AppendTurnDigestAsync(
            item.UserId,
            surfacePath,
            digest,
            cancellationToken);

        await AppendRevisionsAsync(
            store,
            item.UserId,
            surfacePath,
            current.Document,
            outcome,
            digestId,
            cancellationToken);

        var counters = await shadowCounters.UpdateAsync(item.UserId, events, cancellationToken);
        stopwatch.Stop();

        var record = new AgentRunRecord
        {
            RunId = FileAgentRunRecorder.NewRunId(),
            Ts = DateTime.UtcNow.ToString("O"),
            Tier = 1,
            Agent = FoundryCollaborationAgents.ProfileUpdaterAgentName,
            Source = outcome.Profile.Source,
            UserId = item.UserId.ToString("D"),
            SessionId = context.SessionId,
            Trigger = context.Trigger,
            PromptVersion = FileAgentRunRecorder.Hash(
                context.PromptOverride ?? FoundryCollaborationAgents.ProfileUpdaterInstructions),
            ContextHash = context.ContextHash,
            GlossaryVersion = glossary.Version,
            InputInteractionIds = events
                .Select(e => e.Id)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Cast<string>()
                .ToList(),
            ProfileVersionIn = current.Version,
            ProfileVersionOut = versionOut ?? current.Version,
            RawRequest = outcome.RawRequest ?? string.Empty,
            RawResponse = outcome.RawResponse,
            ValidationResult = outcome.ValidationResult,
            ProfileDiff = profileDiff,
            ShadowCounters = counters,
            LatencyMs = stopwatch.ElapsedMilliseconds,
        };
        await runRecorder.WriteAsync(record, cancellationToken);
        return record;
    }

    /// <summary>
    /// Derives Revisions rows (unified log of revisions AND challenges-that-held) from
    /// changelog entries that are new in the updated document, falling back to the
    /// updater's change reason when the document did not change structurally.
    /// </summary>
    private static async Task AppendRevisionsAsync(
        IBeliefProfileStore store,
        Guid userId,
        string surfacePath,
        string previousDocument,
        CollaborationProfileUpdateResult outcome,
        long? digestId,
        CancellationToken cancellationToken)
    {
        var before = BeliefDocumentFormat
            .ParseChangelogEntries(BeliefDocumentFormat.ChangelogRegion(previousDocument))
            .Select(e => e.Body)
            .ToHashSet(StringComparer.Ordinal);
        var after = BeliefDocumentFormat
            .ParseChangelogEntries(BeliefDocumentFormat.ChangelogRegion(outcome.Profile.Document));

        var appended = false;
        foreach (var entry in after.Where(e => !before.Contains(e.Body)))
        {
            appended = true;
            await store.AppendRevisionAsync(
                userId,
                surfacePath,
                entry.Kind,
                entry.Body,
                digestId,
                cancellationToken);
        }

        if (!appended && !string.IsNullOrWhiteSpace(outcome.ChangeReason))
        {
            await store.AppendRevisionAsync(
                userId,
                surfacePath,
                "revised",
                outcome.ChangeReason,
                digestId,
                cancellationToken);
        }
    }
}
