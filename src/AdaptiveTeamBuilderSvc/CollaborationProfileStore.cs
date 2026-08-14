using AdaptiveTeamBuilder.Data;
using AdaptiveTeamBuilder.Data.Contracts;
using AdaptiveTeamBuilder.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdaptiveTeamBuilderSvc;

public interface ICollaborationProfileStore
{
    Task<CollaborationTendencyBundleDto> GetAsync(Guid userId, CancellationToken cancellationToken);

    Task SaveAsync(
        Guid userId,
        CollaborationTendencyBundleDto profile,
        CancellationToken cancellationToken);

    Task<long?> AppendTurnDigestAsync(
        Guid userId,
        string? digest,
        CancellationToken cancellationToken);

    Task AppendChangeLogAsync(
        Guid userId,
        string? reason,
        long? turnDigestId,
        CancellationToken cancellationToken);
}

public sealed class EfCollaborationProfileStore(AdaptiveTeamBuilderDbContext db)
    : ICollaborationProfileStore
{
    /// <summary>Number of most-recent digests surfaced to the updater/advisor prompts.</summary>
    public const int MaxRecentTurnDigests = 5;

    public const string DefaultAppTendencyProse =
        "On Select Contract, start with numeric signal values and summary cards. "
        + "Expand a card for extended staffing/scope detail before selecting. "
        + "No preferred commercial signal or graph-vs-values preference yet.";

    public async Task<CollaborationTendencyBundleDto> GetAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var state = await db.UserCollaborationStates.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        // Newest N rows, then reversed to oldest->newest for prompt formatting.
        var recent = await db.CollaborationTurnDigests.AsNoTracking()
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.Sequence)
            .Take(MaxRecentTurnDigests)
            .Select(d => d.DigestText)
            .ToListAsync(cancellationToken);
        recent.Reverse();

        return ToBundle(state, recent);
    }

    public async Task SaveAsync(
        Guid userId,
        CollaborationTendencyBundleDto profile,
        CancellationToken cancellationToken)
    {
        var stored = await db.UserCollaborationStates
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        if (stored is null)
        {
            stored = new UserCollaborationState
            {
                UserId = userId,
            };
            db.UserCollaborationStates.Add(stored);
        }

        stored.TendencyProse = profile.UserOverride;
        stored.TendencySource = profile.Source;
        stored.UpdatedAt = profile.UpdatedAt ?? DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<long?> AppendTurnDigestAsync(
        Guid userId,
        string? digest,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(digest))
        {
            return null;
        }

        var nextSequence = await db.CollaborationTurnDigests
            .Where(d => d.UserId == userId)
            .MaxAsync(d => (int?)d.Sequence, cancellationToken) ?? 0;

        var entity = new CollaborationTurnDigest
        {
            UserId = userId,
            Sequence = nextSequence + 1,
            CreatedAt = DateTime.UtcNow,
            DigestText = digest.Trim(),
        };
        db.CollaborationTurnDigests.Add(entity);

        await db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task AppendChangeLogAsync(
        Guid userId,
        string? reason,
        long? turnDigestId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return;
        }

        db.CollaborationStateChangeLogs.Add(new CollaborationStateChangeLog
        {
            UserId = userId,
            TurnDigestId = turnDigestId,
            Reason = reason.Trim(),
            CreatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    private static CollaborationTendencyBundleDto ToBundle(
        UserCollaborationState? state,
        IReadOnlyList<string> digests)
    {
        if (state?.TendencyProse is { Length: > 0 })
        {
            return new CollaborationTendencyBundleDto(
                DefaultAppTendencyProse,
                state.TendencyProse,
                state.UpdatedAt,
                state.TendencySource,
                digests);
        }

        return new CollaborationTendencyBundleDto(
            DefaultAppTendencyProse,
            null,
            null,
            "app",
            digests);
    }
}
