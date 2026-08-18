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
    string? PromptOverride = null,
    string? RunId = null);

public record CollaborationProfileUpdateWorkItem(
    Guid UserId,
    IReadOnlyList<InteractionDto> Events,
    CollaborationProfileUpdateContext? Context = null,
    long? TurnDigestId = null,
    bool TurnDigestRecorded = false,
    DateTimeOffset? EnqueuedAtUtc = null,
    DateTimeOffset? OldestEnqueuedAtUtc = null,
    int SupersededWorkItemCount = 0,
    IReadOnlyList<string>? SupersededInteractionIds = null);

public interface ICollaborationProfileUpdateQueue
{
    ValueTask EnqueueAsync(
        CollaborationProfileUpdateWorkItem item,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<CollaborationProfileUpdateWorkItem> ReadAllAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Drains work that accumulated while Foundry was processing. Callers may coalesce
    /// superseded items because each decision digest and raw interaction is already persisted.
    /// </summary>
    IReadOnlyList<CollaborationProfileUpdateWorkItem> DrainPending();
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
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var stamped = item with
        {
            EnqueuedAtUtc = item.EnqueuedAtUtc ?? now,
            OldestEnqueuedAtUtc = item.OldestEnqueuedAtUtc ?? item.EnqueuedAtUtc ?? now,
        };
        return _channel.Writer.WriteAsync(stamped, cancellationToken);
    }

    public IAsyncEnumerable<CollaborationProfileUpdateWorkItem> ReadAllAsync(
        CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);

    public IReadOnlyList<CollaborationProfileUpdateWorkItem> DrainPending()
    {
        var pending = new List<CollaborationProfileUpdateWorkItem>();
        while (_channel.Reader.TryRead(out var item))
        {
            pending.Add(item);
        }

        return pending;
    }
}

public sealed class CollaborationProfileUpdateBackgroundService(
    ICollaborationProfileUpdateQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<CollaborationProfileUpdateBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var first in queue.ReadAllAsync(stoppingToken))
        {
            // A profile update can take tens of seconds. Keep only the newest queued item for
            // each user; earlier turns are already represented by persisted decision digests.
            var latestByUser = new Dictionary<Guid, CollaborationProfileUpdateWorkItem>
            {
                [first.UserId] = first,
            };
            foreach (var pending in queue.DrainPending())
            {
                latestByUser[pending.UserId] = latestByUser.TryGetValue(pending.UserId, out var older)
                    ? Coalesce(older, pending)
                    : pending;
            }

            foreach (var item in latestByUser.Values)
            {
                try
                {
                    await ProcessAsync(item, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
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
    }

    private static CollaborationProfileUpdateWorkItem Coalesce(
        CollaborationProfileUpdateWorkItem older,
        CollaborationProfileUpdateWorkItem newer)
    {
        var supersededIds = (newer.SupersededInteractionIds ?? [])
            .Concat(older.SupersededInteractionIds ?? [])
            .Concat(older.Events
                .Select(e => e.Id)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Cast<string>())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var oldest = new[]
            {
                older.OldestEnqueuedAtUtc ?? older.EnqueuedAtUtc,
                newer.OldestEnqueuedAtUtc ?? newer.EnqueuedAtUtc,
            }
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .DefaultIfEmpty(DateTimeOffset.UtcNow)
            .Min();

        return newer with
        {
            OldestEnqueuedAtUtc = oldest,
            SupersededWorkItemCount = newer.SupersededWorkItemCount
                + older.SupersededWorkItemCount
                + 1,
            SupersededInteractionIds = supersededIds,
        };
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

        var processingStarted = DateTimeOffset.UtcNow;
        var selectedEnqueued = item.EnqueuedAtUtc ?? processingStarted;
        var oldestEnqueued = item.OldestEnqueuedAtUtc ?? selectedEnqueued;
        var processingStopwatch = Stopwatch.StartNew();
        var timings = new AgentRunTimingRecord();
        var runId = FileAgentRunRecorder.NewRunId();
        var contextSeed = item.Context;
        var runRecord = new AgentRunRecord
        {
            RunId = runId,
            Ts = processingStarted.ToString("O"),
            Tier = 1,
            Agent = FoundryCollaborationAgents.ProfileUpdaterAgentName,
            Source = "foundry",
            UserId = item.UserId.ToString("D"),
            SessionId = contextSeed?.SessionId,
            Trigger = contextSeed?.Trigger ?? "flush-on-action",
            PromptVersion = FileAgentRunRecorder.Hash(
                contextSeed?.PromptOverride ?? FoundryCollaborationAgents.ProfileUpdaterInstructions),
            ContextHash = contextSeed?.ContextHash,
            GlossaryVersion = glossary.Version,
            InputInteractionIds = item.Events
                .Select(e => e.Id)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Cast<string>()
                .ToList(),
            Queue = new AgentRunQueueRecord
            {
                SelectedItemEnqueuedAtUtc = selectedEnqueued.ToString("O"),
                OldestItemEnqueuedAtUtc = oldestEnqueued.ToString("O"),
                ProcessingStartedAtUtc = processingStarted.ToString("O"),
                SelectedItemQueueWaitMs = ElapsedMilliseconds(selectedEnqueued, processingStarted),
                OldestItemQueueWaitMs = ElapsedMilliseconds(oldestEnqueued, processingStarted),
                SupersededWorkItemCount = item.SupersededWorkItemCount,
                SupersededInteractionIds = item.SupersededInteractionIds ?? [],
            },
            Timings = timings,
        };

        try
        {
            var phase = Stopwatch.StartNew();
            var current = await store.GetAsync(item.UserId, cancellationToken);
            phase.Stop();
            timings.ProfileLoadMs = phase.ElapsedMilliseconds;
            runRecord.ProfileVersionIn = current.Version;

            phase.Restart();
            var context = item.Context is null
                ? new CollaborationProfileUpdateContext(
                    null,
                    null,
                    null,
                    null,
                    RecentTurnDigests: current.RecentTurnDigests,
                    RunId: runId)
                : item.Context with
                {
                    RecentTurnDigests = current.RecentTurnDigests,
                    RunId = runId,
                };
            var events = CollaborationContextFormatter.FlagReversals(item.Events);
            phase.Stop();
            timings.ContextPreparationMs = phase.ElapsedMilliseconds;
            runRecord.InputInteractionIds = events
                .Select(e => e.Id)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Cast<string>()
                .ToList();

            phase.Restart();
            var outcome = await updater.UpdateFromObservationsAsync(
                current,
                events,
                context,
                cancellationToken);
            phase.Stop();
            timings.AgentUpdateMs = phase.ElapsedMilliseconds;
            runRecord.Source = outcome.Profile.Source;
            runRecord.RawRequest = outcome.RawRequest ?? string.Empty;
            runRecord.RawResponse = outcome.RawResponse;
            runRecord.ValidationResult = outcome.ValidationResult;
            runRecord.ProfileUpdateDiagnostics = outcome.Diagnostics;
            runRecord.Model = outcome.Diagnostics?.Model;
            runRecord.RunOptions = outcome.Diagnostics?.RunOptions;

            phase.Restart();
            var surfacePath = context.SurfacePath ?? BeliefDocumentFormat.ContractsListScope;
            var digest = CollaborationContextFormatter.FormatDecisionTurnDigest(
                context.Controls,
                context.ViewState,
                events,
                CollaborationContextFormatter.ActiveProfileSummary(current),
                surfacePath);
            phase.Stop();
            timings.ContextPreparationMs += phase.ElapsedMilliseconds;

            int? versionOut = null;
            string? profileDiff = null;
            var documentChanged = !string.Equals(
                outcome.Profile.Document,
                current.Document,
                StringComparison.Ordinal);
            if (documentChanged && outcome.ValidationResult != "rejected")
            {
                phase.Restart();
                versionOut = await store.SaveAsync(
                    item.UserId,
                    outcome.Profile.Document,
                    outcome.Profile.Source,
                    cancellationToken);
                phase.Stop();
                timings.ProfileSaveMs = phase.ElapsedMilliseconds;
                profileDiff = BeliefDocumentFormat.UnifiedDiff(
                    current.Document,
                    outcome.Profile.Document);
            }
            runRecord.ProfileVersionOut = versionOut ?? current.Version;
            runRecord.ProfileDiff = profileDiff;

            long? digestId;
            if (item.TurnDigestRecorded)
            {
                digestId = item.TurnDigestId;
            }
            else
            {
                phase.Restart();
                digestId = await store.AppendTurnDigestAsync(
                    item.UserId,
                    surfacePath,
                    digest,
                    cancellationToken);
                phase.Stop();
                timings.TurnDigestPersistenceMs = phase.ElapsedMilliseconds;
            }

            phase.Restart();
            await AppendRevisionsAsync(
                store,
                item.UserId,
                surfacePath,
                current.Document,
                outcome,
                digestId,
                cancellationToken);
            phase.Stop();
            timings.RevisionPersistenceMs = phase.ElapsedMilliseconds;

            phase.Restart();
            runRecord.ShadowCounters = await shadowCounters.UpdateAsync(
                item.UserId,
                events,
                cancellationToken);
            phase.Stop();
            timings.ShadowCounterPersistenceMs = phase.ElapsedMilliseconds;

            CompleteTimings(timings, processingStopwatch, selectedEnqueued);
            runRecord.LatencyMs = timings.ProcessingTotalMs;
            await runRecorder.WriteAsync(runRecord, cancellationToken);
            return runRecord;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            CompleteTimings(timings, processingStopwatch, selectedEnqueued);
            runRecord.Source = "error";
            runRecord.ValidationResult ??= "error";
            runRecord.Error = ex.ToString();
            runRecord.LatencyMs = timings.ProcessingTotalMs;
            await runRecorder.WriteAsync(runRecord, CancellationToken.None);
            throw;
        }
    }

    private static long ElapsedMilliseconds(DateTimeOffset from, DateTimeOffset to) =>
        Math.Max(0, (long)(to - from).TotalMilliseconds);

    private static void CompleteTimings(
        AgentRunTimingRecord timings,
        Stopwatch processingStopwatch,
        DateTimeOffset selectedEnqueued)
    {
        processingStopwatch.Stop();
        timings.ProcessingTotalMs = processingStopwatch.ElapsedMilliseconds;
        timings.EndToEndFromSelectedEnqueueMs = ElapsedMilliseconds(
            selectedEnqueued,
            DateTimeOffset.UtcNow);
        var attributed = timings.ProfileLoadMs
            + timings.ContextPreparationMs
            + timings.AgentUpdateMs
            + timings.ProfileSaveMs
            + timings.TurnDigestPersistenceMs
            + timings.RevisionPersistenceMs
            + timings.ShadowCounterPersistenceMs;
        timings.UnattributedMs = Math.Max(0, timings.ProcessingTotalMs - attributed);
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
