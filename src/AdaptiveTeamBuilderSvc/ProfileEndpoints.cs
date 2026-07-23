using AdaptiveTeamBuilder.Data;
using AdaptiveTeamBuilder.Data.Contracts;
using AdaptiveTeamBuilder.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace AdaptiveTeamBuilderSvc;

public static class ProfileEndpoints
{
    public static RouteGroupBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profiles")
            .WithTags("Profiles")
            .RequireAuthorization("AccessAsUser");

        group.MapGet("/", SearchProfilesAsync);
        group.MapGet("/{id:guid}", GetProfileByIdAsync);

        return group;
    }

    private static async Task<IResult> SearchProfilesAsync(
        AdaptiveTeamBuilderDbContext db,
        string? q,
        string? positionTypes,
        CancellationToken cancellationToken)
    {
        var selectedTypes = ParsePositionTypes(positionTypes, out var invalidType);
        if (invalidType is not null)
        {
            return Results.BadRequest(new
            {
                error = $"Invalid position type '{invalidType}'. Allowed: {string.Join(", ", PositionTypes.All)}",
            });
        }

        var hasSearchCriteria =
            !string.IsNullOrWhiteSpace(q) || selectedTypes.Count > 0;

        if (!hasSearchCriteria)
        {
            return Results.Ok(Array.Empty<EmployeeProfileListItemDto>());
        }

        var query = db.EmployeeProfiles.AsNoTracking().AsQueryable();

        if (selectedTypes.Count > 0)
        {
            query = query.Where(p => selectedTypes.Contains(p.PositionType.Code));
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p =>
                p.DisplayName.Contains(term)
                || p.Title.Contains(term)
                || (p.Summary != null && p.Summary.Contains(term))
                || (p.ExperienceLevel != null && (
                    p.ExperienceLevel.Code.Contains(term) || p.ExperienceLevel.Name.Contains(term)))
                || (p.RoleSpecialty != null && (
                    p.RoleSpecialty.Code.Contains(term) || p.RoleSpecialty.Name.Contains(term)))
                || p.EmployeeProfileSkills.Any(s => s.Skill.Name.Contains(term)));
        }

        var results = await query
            .OrderBy(p => p.DisplayName)
            .Select(p => new EmployeeProfileListItemDto(
                p.Id,
                p.DisplayName,
                p.PositionType.Code,
                p.RoleSpecialty != null ? p.RoleSpecialty.Code : null,
                p.ExperienceLevel != null ? p.ExperienceLevel.Code : null,
                p.Title,
                p.Location,
                p.Availability))
            .ToListAsync(cancellationToken);

        return Results.Ok(results);
    }

    private static async Task<IResult> GetProfileByIdAsync(
        Guid id,
        AdaptiveTeamBuilderDbContext db,
        CancellationToken cancellationToken)
    {
        var profile = await db.EmployeeProfiles
            .AsNoTracking()
            .Include(p => p.PositionType)
            .Include(p => p.ExperienceLevel)
            .Include(p => p.RoleSpecialty)
            .Include(p => p.EmployeeProfileSkills)
                .ThenInclude(ps => ps.Skill)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (profile is null)
        {
            return Results.NotFound();
        }

        var dto = new EmployeeProfileDto(
            profile.Id,
            profile.FirstName,
            profile.LastName,
            profile.DisplayName,
            profile.PositionType.Code,
            profile.RoleSpecialty?.Code,
            profile.ExperienceLevel?.Code,
            profile.Title,
            profile.Summary,
            profile.EmployeeProfileSkills
                .Select(ps => ps.Skill.Name)
                .OrderBy(name => name)
                .ToArray(),
            profile.YearsExperience,
            profile.Location,
            profile.Availability,
            profile.CreatedDate,
            profile.ModifiedDate);

        return Results.Ok(dto);
    }

    private static List<string> ParsePositionTypes(string? positionTypes, out string? invalidType)
    {
        invalidType = null;
        var selected = new List<string>();

        if (string.IsNullOrWhiteSpace(positionTypes))
        {
            return selected;
        }

        foreach (var raw in positionTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var normalized = PositionTypes.Normalize(raw);
            if (normalized is null)
            {
                invalidType = raw;
                return selected;
            }

            if (!selected.Contains(normalized, StringComparer.Ordinal))
            {
                selected.Add(normalized);
            }
        }

        return selected;
    }
}
