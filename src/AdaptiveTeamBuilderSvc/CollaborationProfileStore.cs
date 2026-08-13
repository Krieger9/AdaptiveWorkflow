using System.Text.Json;
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
    public const int MaxRecentTurnDigests = 5;

    public const string DefaultAppTendencyProse =
        "On Select Contract, start with numeric signal values and summary cards. "
        + "Expand a card for extended staffing/scope detail before selecting. "
        + "No preferred commercial signal or graph-vs-values preference yet.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

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
        stored.RecentTurnDigestsJson = SerializeDigests(profile.RecentTurnDigests);

        await db.SaveChangesAsync(cancellationToken);
    }

    public static IReadOnlyList<string> AppendDigest(
        IReadOnlyList<string>? existing,
        string? digest,
        int max = MaxRecentTurnDigests)
    {
        if (string.IsNullOrWhiteSpace(digest))
        {
            return existing ?? [];
        }

        var list = existing?.ToList() ?? [];
        list.Add(digest.Trim());
        while (list.Count > max)
        {
            list.RemoveAt(0);
        }

        return list;
    }

    private static CollaborationTendencyBundleDto ToBundle(UserCollaborationState? state)
    {
        var digests = DeserializeDigests(state?.RecentTurnDigestsJson);

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

    private static IReadOnlyList<string> DeserializeDigests(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<List<string>>(json, JsonOptions);
            return parsed is { Count: > 0 } ? parsed : [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? SerializeDigests(IReadOnlyList<string>? digests)
    {
        if (digests is null || digests.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(digests.ToList(), JsonOptions);
    }
}
