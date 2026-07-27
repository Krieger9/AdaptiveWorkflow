using System.Security.Claims;
using AdaptiveTeamBuilder.Data;
using AdaptiveTeamBuilder.Data.Contracts;
using Microsoft.EntityFrameworkCore;

namespace AdaptiveTeamBuilderSvc;

public static class CollaborationEndpoints
{
    private const string ObjectIdClaim =
        "http://schemas.microsoft.com/identity/claims/objectidentifier";

    public static RouteGroupBuilder MapCollaborationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/collaboration")
            .WithTags("Collaboration")
            .RequireAuthorization("AccessAsUser");

        group.MapGet("/profile", GetProfileAsync);
        // Alias for earlier clients / OpenAPI explorers.
        group.MapGet("/tendencies", GetProfileAsync);
        group.MapPost("/advise", AdviseAsync);
        group.MapPost("/observations", SubmitObservationsAsync);

        return group;
    }

    private static async Task<IResult> GetProfileAsync(
        ClaimsPrincipal principal,
        AdaptiveTeamBuilderDbContext db,
        ICollaborationProfileStore profileStore,
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

        var tendencies = await profileStore.GetAsync(user.Id, cancellationToken);
        return Results.Ok(new CollaborationProfileResponse(tendencies));
    }

    private static async Task<IResult> AdviseAsync(
        CollaborationAdviseRequest request,
        ClaimsPrincipal principal,
        AdaptiveTeamBuilderDbContext db,
        ICollaborationProfileStore profileStore,
        ICollaborationAdvisor advisor,
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

        var profile = await profileStore.GetAsync(user.Id, cancellationToken);
        var response = await advisor.AdviseAsync(request, profile, cancellationToken);
        return Results.Ok(response);
    }

    private static async Task<IResult> SubmitObservationsAsync(
        CollaborationObservationsRequest request,
        ClaimsPrincipal principal,
        AdaptiveTeamBuilderDbContext db,
        ICollaborationProfileStore profileStore,
        ICollaborationAdvisor advisor,
        ICollaborationProfileUpdateQueue updateQueue,
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

        if (request.UserId != user.Id)
        {
            return Results.Json(
                new { error = "UserId does not match the authenticated user." },
                statusCode: StatusCodes.Status403Forbidden);
        }

        var profile = await profileStore.GetAsync(user.Id, cancellationToken);
        var adviseRequest = new CollaborationAdviseRequest(
            request.App,
            request.Screen,
            request.Controls,
            request.Events);
        var advice = await advisor.AdviseAsync(adviseRequest, profile, cancellationToken);

        if (request.Events.Count > 0)
        {
            await updateQueue.EnqueueAsync(
                new CollaborationProfileUpdateWorkItem(
                    user.Id,
                    request.Events,
                    new CollaborationProfileUpdateContext(
                        request.Screen.ScreenId,
                        request.Screen.Title,
                        request.Screen.ViewState,
                        request.Screen.Annotations,
                        request.App.ContractCount)),
                cancellationToken);
        }

        return Results.Ok(new CollaborationObservationsResponse(
            user.Id,
            request.Events.Count,
            "accepted",
            advice.PromptPreview,
            advice.Suggestions,
            advice.PreferredLayout));
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
