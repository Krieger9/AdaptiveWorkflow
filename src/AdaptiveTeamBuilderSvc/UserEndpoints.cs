using System.Security.Claims;
using AdaptiveTeamBuilder.Data;
using AdaptiveTeamBuilder.Data.Contracts;
using AdaptiveTeamBuilder.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdaptiveTeamBuilderSvc;

public static class UserEndpoints
{
    private const string ObjectIdClaim =
        "http://schemas.microsoft.com/identity/claims/objectidentifier";

    public static RouteGroupBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization("AccessAsUser");

        group.MapPost("/me/session", EnsureSessionAsync);
        group.MapGet("/me", GetMeAsync);
        group.MapPut("/me/profile", UpdateMyProfileAsync);

        return group;
    }

    private static async Task<IResult> EnsureSessionAsync(
        ClaimsPrincipal principal,
        AdaptiveTeamBuilderDbContext db,
        CancellationToken cancellationToken)
    {
        if (!TryGetIdentity(principal, out var objectId, out var userName, out var tokenDisplayName))
        {
            return Results.Unauthorized();
        }

        var now = DateTime.UtcNow;
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.AzureAdObjectId == objectId, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                AzureAdObjectId = objectId,
                UserName = userName,
                DisplayName = tokenDisplayName,
                CreatedDate = now,
                ModifiedDate = now,
                LastLoggedInDate = now,
            };
            db.Users.Add(user);
        }
        else
        {
            if (!string.Equals(user.UserName, userName, StringComparison.Ordinal))
            {
                user.UserName = userName;
            }

            user.LastLoggedInDate = now;
            user.ModifiedDate = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToDto(user));
    }

    private static async Task<IResult> GetMeAsync(
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

        return user is null ? Results.NotFound() : Results.Ok(ToDto(user));
    }

    private static async Task<IResult> UpdateMyProfileAsync(
        UpdateUserProfileRequest request,
        ClaimsPrincipal principal,
        AdaptiveTeamBuilderDbContext db,
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

        user.FirstName = NormalizeOptional(request.FirstName, 100);
        user.LastName = NormalizeOptional(request.LastName, 100);
        user.DisplayName = NormalizeOptional(request.DisplayName, 200);
        user.ModifiedDate = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToDto(user));
    }

    private static bool TryGetIdentity(
        ClaimsPrincipal principal,
        out string objectId,
        out string userName,
        out string? displayName)
    {
        objectId = string.Empty;
        userName = string.Empty;
        displayName = null;

        if (!TryGetObjectId(principal, out objectId))
        {
            return false;
        }

        userName =
            FirstClaim(principal, "preferred_username")
            ?? FirstClaim(principal, ClaimTypes.Upn)
            ?? FirstClaim(principal, "upn")
            ?? FirstClaim(principal, ClaimTypes.Email)
            ?? FirstClaim(principal, "unique_name")
            ?? objectId;

        displayName = NormalizeOptional(
            FirstClaim(principal, "name") ?? FirstClaim(principal, ClaimTypes.Name),
            200);

        return true;
    }

    private static bool TryGetObjectId(ClaimsPrincipal principal, out string objectId)
    {
        objectId =
            FirstClaim(principal, "oid")
            ?? FirstClaim(principal, ObjectIdClaim)
            ?? string.Empty;

        return !string.IsNullOrWhiteSpace(objectId);
    }

    private static string? FirstClaim(ClaimsPrincipal principal, string type) =>
        principal.FindFirst(type)?.Value;

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static UserDto ToDto(User user) => new(
        user.Id,
        user.AzureAdObjectId,
        user.UserName,
        user.FirstName,
        user.LastName,
        user.DisplayName,
        user.CreatedDate,
        user.ModifiedDate,
        user.LastLoggedInDate);
}
