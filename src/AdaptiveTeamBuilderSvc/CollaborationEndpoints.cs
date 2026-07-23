using System.Security.Claims;
using AdaptiveTeamBuilder.Data;
using AdaptiveTeamBuilder.Data.Contracts;
using AdaptiveTeamBuilder.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdaptiveTeamBuilderSvc;

public static class CollaborationEndpoints
{
    private const string ObjectIdClaim =
        "http://schemas.microsoft.com/identity/claims/objectidentifier";

    public const string DefaultAppTendencyProse =
        "On Select Contract, examine cards left-to-right in grid order. "
        + "Expand details before choosing. No preferred commercial signal yet.";

    public static RouteGroupBuilder MapCollaborationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/collaboration")
            .WithTags("Collaboration")
            .RequireAuthorization("AccessAsUser");

        group.MapGet("/tendencies", GetTendenciesAsync);
        group.MapPost("/advise", AdviseAsync);

        return group;
    }

    private static async Task<IResult> GetTendenciesAsync(
        ClaimsPrincipal principal,
        AdaptiveTeamBuilderDbContext db,
        CancellationToken cancellationToken)
    {
        if (!TryGetObjectId(principal, out var objectId))
        {
            return Results.Unauthorized();
        }

        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.AzureAdObjectId == objectId, cancellationToken);
        if (user is null)
        {
            return Results.NotFound();
        }

        var state = await db.UserCollaborationStates.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == user.Id, cancellationToken);

        return Results.Ok(new CollaborationTendenciesResponse(ToBundle(state)));
    }

    private static async Task<IResult> AdviseAsync(
        CollaborationAdviseRequest request,
        ClaimsPrincipal principal,
        AdaptiveTeamBuilderDbContext db,
        ICollaborationAdvisor advisor,
        CancellationToken cancellationToken)
    {
        if (!TryGetObjectId(principal, out var objectId))
        {
            return Results.Unauthorized();
        }

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.AzureAdObjectId == objectId, cancellationToken);
        if (user is null)
        {
            return Results.NotFound();
        }

        var stored = await db.UserCollaborationStates
            .FirstOrDefaultAsync(s => s.UserId == user.Id, cancellationToken);

        // Prefer server-stored override when the client only has app defaults.
        var tendencies = MergeTendencies(request.Tendencies, stored);
        var enrichedRequest = request with { Tendencies = tendencies };

        var response = advisor.Advise(enrichedRequest);

        if (stored is null)
        {
            stored = new UserCollaborationState
            {
                UserId = user.Id,
            };
            db.UserCollaborationStates.Add(stored);
        }

        stored.TendencyProse = response.UpdatedTendencies.UserOverride;
        stored.TendencySource = response.UpdatedTendencies.Source;
        stored.UpdatedAt = response.UpdatedTendencies.UpdatedAt ?? DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(response);
    }

    private static CollaborationTendencyBundleDto MergeTendencies(
        CollaborationTendencyBundleDto fromClient,
        UserCollaborationState? stored)
    {
        var appDefaults = string.IsNullOrWhiteSpace(fromClient.AppDefaults)
            ? DefaultAppTendencyProse
            : fromClient.AppDefaults;

        if (!string.IsNullOrWhiteSpace(fromClient.UserOverride))
        {
            return fromClient with { AppDefaults = appDefaults };
        }

        if (stored?.TendencyProse is { Length: > 0 })
        {
            return new CollaborationTendencyBundleDto(
                appDefaults,
                stored.TendencyProse,
                stored.UpdatedAt,
                stored.TendencySource);
        }

        return new CollaborationTendencyBundleDto(
            appDefaults,
            null,
            null,
            "app");
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

    private static bool TryGetObjectId(ClaimsPrincipal principal, out string objectId)
    {
        objectId =
            principal.FindFirst("oid")?.Value
            ?? principal.FindFirst(ObjectIdClaim)?.Value
            ?? string.Empty;

        return !string.IsNullOrWhiteSpace(objectId);
    }
}
