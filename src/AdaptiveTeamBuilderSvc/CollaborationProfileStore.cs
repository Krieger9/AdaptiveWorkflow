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
}

public sealed class EfCollaborationProfileStore(AdaptiveTeamBuilderDbContext db)
    : ICollaborationProfileStore
{
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

        return ToBundle(state);
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

    private static CollaborationTendencyBundleDto ToBundle(UserCollaborationState? state)
    {
        if (state?.TendencyProse is { Length: > 0 })
        {
            return new CollaborationTendencyBundleDto(
                DefaultAppTendencyProse,
                state.TendencyProse,
                state.UpdatedAt,
                state.TendencySource);
        }

        return new CollaborationTendencyBundleDto(
            DefaultAppTendencyProse,
            null,
            null,
            "app");
    }
}
