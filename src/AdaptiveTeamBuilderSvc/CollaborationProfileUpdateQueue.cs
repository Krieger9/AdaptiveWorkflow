using System.Threading.Channels;
using AdaptiveTeamBuilder.Data.Contracts;

namespace AdaptiveTeamBuilderSvc;

public record CollaborationProfileUpdateContext(
    string? ScreenId,
    string? ScreenTitle,
    CollaborationViewStateDto? ViewState,
    IReadOnlyDictionary<string, string>? ScreenAnnotations,
    int? VisibleControlCount = null,
    IReadOnlyList<CollaborationControlSnapshotDto>? Controls = null,
    IReadOnlyList<string>? RecentTurnDigests = null);

public record CollaborationProfileUpdateWorkItem(
    Guid UserId,
    IReadOnlyList<CollaborationInteractionEventDto> Events,
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
                await using var scope = scopeFactory.CreateAsyncScope();
                var store = scope.ServiceProvider.GetRequiredService<ICollaborationProfileStore>();
                var updater = scope.ServiceProvider.GetRequiredService<ICollaborationProfileUpdater>();

                var current = await store.GetAsync(item.UserId, stoppingToken);
                var context = item.Context is null
                    ? new CollaborationProfileUpdateContext(
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        current.RecentTurnDigests)
                    : item.Context with { RecentTurnDigests = current.RecentTurnDigests };

                var updated = await updater.UpdateFromObservationsAsync(
                    current,
                    item.Events,
                    context,
                    stoppingToken);

                var digest = CollaborationContextFormatter.FormatDecisionTurnDigest(
                    context.Controls,
                    context.ViewState,
                    item.Events,
                    CollaborationContextFormatter.ActiveProfileSummary(current),
                    context.ScreenId);
                var digests = EfCollaborationProfileStore.AppendDigest(
                    current.RecentTurnDigests,
                    digest);

                await store.SaveAsync(
                    item.UserId,
                    updated with { RecentTurnDigests = digests },
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to update collaboration profile for user {UserId} from {EventCount} observation(s).",
                    item.UserId,
                    item.Events.Count);
            }
        }
    }
}
