using System.Security.Claims;
using Microsoft.Identity.Web;

namespace AdaptiveTeamBuilderSvc;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .RequireAuthorization("AccessAsUser");

        group.MapGet("/me", (ClaimsPrincipal principal) =>
        {
            var scopes = principal.FindFirst("scp")?.Value
                ?? principal.FindFirst("http://schemas.microsoft.com/identity/claims/scope")?.Value;

            return Results.Ok(new
            {
                authenticated = principal.Identity?.IsAuthenticated ?? false,
                objectId = principal.GetObjectId(),
                name = principal.GetDisplayName(),
                userName = principal.FindFirst("preferred_username")?.Value
                    ?? principal.FindFirst(ClaimTypes.Upn)?.Value
                    ?? principal.FindFirst("upn")?.Value,
                audience = principal.FindFirst("aud")?.Value,
                issuer = principal.FindFirst("iss")?.Value,
                scopes,
                claims = principal.Claims
                    .Select(c => new { c.Type, c.Value })
                    .OrderBy(c => c.Type)
                    .ToArray(),
            });
        });

        return group;
    }
}
