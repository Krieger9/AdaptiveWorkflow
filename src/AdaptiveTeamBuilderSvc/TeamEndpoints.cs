using AdaptiveTeamBuilder.Data;
using AdaptiveTeamBuilder.Data.Contracts;
using AdaptiveTeamBuilder.Data.Entities;
using AdaptiveTeamBuilder.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace AdaptiveTeamBuilderSvc;

public static class TeamEndpoints
{
    public static RouteGroupBuilder MapTeamEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/teams")
            .WithTags("Teams")
            .RequireAuthorization("AccessAsUser");

        group.MapGet("/", ListTeamsAsync);
        group.MapPost("/", CreateTeamAsync);
        group.MapGet("/{teamId:guid}", GetTeamAsync);
        group.MapPut("/{teamId:guid}", RenameTeamAsync);
        group.MapPut("/{teamId:guid}/requirements", UpsertRequirementsAsync);
        group.MapPost("/{teamId:guid}/members", AddMemberAsync);
        group.MapDelete("/{teamId:guid}/members/{employeeProfileId:guid}", RemoveMemberAsync);
        group.MapPost("/{teamId:guid}/hidden", HideProfileAsync);
        group.MapDelete("/{teamId:guid}/hidden/{employeeProfileId:guid}", UnhideProfileAsync);

        return group;
    }

    private static async Task<IResult> ListTeamsAsync(
        Guid? contractId,
        AdaptiveTeamBuilderDbContext db,
        CancellationToken cancellationToken)
    {
        if (contractId is null)
        {
            return Results.BadRequest(new { error = "contractId query parameter is required." });
        }

        var contractExists = await db.Contracts.AnyAsync(c => c.Id == contractId, cancellationToken);
        if (!contractExists)
        {
            return Results.NotFound(new { error = "Contract was not found." });
        }

        var teams = await db.Teams.AsNoTracking()
            .Where(t => t.ContractId == contractId)
            .OrderBy(t => t.Name)
            .Select(t => new TeamListItemDto(t.Id, t.Name, t.ContractId))
            .ToListAsync(cancellationToken);

        return Results.Ok(teams);
    }

    private static async Task<IResult> CreateTeamAsync(
        CreateTeamRequest request,
        AdaptiveTeamBuilderDbContext db,
        CancellationToken cancellationToken)
    {
        var name = NormalizeName(request.Name);
        if (name is null)
        {
            return Results.BadRequest(new { error = "Team name is required." });
        }

        var contractExists = await db.Contracts.AnyAsync(c => c.Id == request.ContractId, cancellationToken);
        if (!contractExists)
        {
            return Results.BadRequest(new { error = "Contract was not found." });
        }

        if (await db.Teams.AnyAsync(
                t => t.ContractId == request.ContractId && t.Name == name,
                cancellationToken))
        {
            return Results.Conflict(new { error = $"A team named '{name}' already exists for this contract." });
        }

        var now = DateTime.UtcNow;
        var positionTypes = await db.PositionTypes
            .OrderBy(p => p.SortOrder)
            .ToListAsync(cancellationToken);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = name,
            ContractId = request.ContractId,
            CreatedDate = now,
            ModifiedDate = now,
            PositionRequirements = positionTypes
                .Select(pt => new TeamPositionRequirement
                {
                    PositionTypeId = pt.Id,
                    RequiredCount = 0,
                })
                .ToList(),
        };

        db.Teams.Add(team);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/teams/{team.Id}", await LoadTeamDetailAsync(db, team.Id, cancellationToken));
    }

    private static async Task<IResult> GetTeamAsync(
        Guid teamId,
        AdaptiveTeamBuilderDbContext db,
        CancellationToken cancellationToken)
    {
        var detail = await LoadTeamDetailAsync(db, teamId, cancellationToken);
        return detail is null ? Results.NotFound() : Results.Ok(detail);
    }

    private static async Task<IResult> RenameTeamAsync(
        Guid teamId,
        RenameTeamRequest request,
        AdaptiveTeamBuilderDbContext db,
        CancellationToken cancellationToken)
    {
        var name = NormalizeName(request.Name);
        if (name is null)
        {
            return Results.BadRequest(new { error = "Team name is required." });
        }

        var team = await db.Teams.FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);
        if (team is null)
        {
            return Results.NotFound();
        }

        if (await db.Teams.AnyAsync(
                t => t.ContractId == team.ContractId && t.Name == name && t.Id != teamId,
                cancellationToken))
        {
            return Results.Conflict(new { error = $"A team named '{name}' already exists for this contract." });
        }

        team.Name = name;
        team.ModifiedDate = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(await LoadTeamDetailAsync(db, teamId, cancellationToken));
    }

    private static async Task<IResult> UpsertRequirementsAsync(
        Guid teamId,
        UpsertTeamRequirementsRequest request,
        AdaptiveTeamBuilderDbContext db,
        CancellationToken cancellationToken)
    {
        var team = await db.Teams
            .Include(t => t.PositionRequirements)
            .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);

        if (team is null)
        {
            return Results.NotFound();
        }

        var positionTypes = await db.PositionTypes.ToListAsync(cancellationToken);
        var byCode = positionTypes.ToDictionary(p => p.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var input in request.Requirements)
        {
            if (!byCode.TryGetValue(input.PositionType, out var positionType))
            {
                return Results.BadRequest(new
                {
                    error = $"Invalid position type '{input.PositionType}'. Allowed: {string.Join(", ", PositionTypes.All)}",
                });
            }

            if (input.RequiredCount < 0)
            {
                return Results.BadRequest(new { error = "RequiredCount must be 0 or greater." });
            }

            var existing = team.PositionRequirements
                .FirstOrDefault(r => r.PositionTypeId == positionType.Id);

            if (existing is null)
            {
                team.PositionRequirements.Add(new TeamPositionRequirement
                {
                    TeamId = team.Id,
                    PositionTypeId = positionType.Id,
                    RequiredCount = input.RequiredCount,
                });
            }
            else
            {
                existing.RequiredCount = input.RequiredCount;
            }
        }

        team.ModifiedDate = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(await LoadTeamDetailAsync(db, teamId, cancellationToken));
    }

    private static async Task<IResult> AddMemberAsync(
        Guid teamId,
        TeamEmployeeRequest request,
        AdaptiveTeamBuilderDbContext db,
        CancellationToken cancellationToken)
    {
        var team = await db.Teams.FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);
        if (team is null)
        {
            return Results.NotFound();
        }

        var profileExists = await db.EmployeeProfiles
            .AnyAsync(p => p.Id == request.EmployeeProfileId, cancellationToken);
        if (!profileExists)
        {
            return Results.BadRequest(new { error = "Employee profile was not found." });
        }

        var alreadyMember = await db.TeamMembers.AnyAsync(
            m => m.TeamId == teamId && m.EmployeeProfileId == request.EmployeeProfileId,
            cancellationToken);
        if (alreadyMember)
        {
            return Results.Ok(await LoadTeamDetailAsync(db, teamId, cancellationToken));
        }

        // Selecting someone who was hidden un-hides them for this team.
        var hidden = await db.TeamHiddenProfiles.FirstOrDefaultAsync(
            h => h.TeamId == teamId && h.EmployeeProfileId == request.EmployeeProfileId,
            cancellationToken);
        if (hidden is not null)
        {
            db.TeamHiddenProfiles.Remove(hidden);
        }

        db.TeamMembers.Add(new TeamMember
        {
            TeamId = teamId,
            EmployeeProfileId = request.EmployeeProfileId,
            AddedDate = DateTime.UtcNow,
        });

        team.ModifiedDate = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(await LoadTeamDetailAsync(db, teamId, cancellationToken));
    }

    private static async Task<IResult> RemoveMemberAsync(
        Guid teamId,
        Guid employeeProfileId,
        AdaptiveTeamBuilderDbContext db,
        CancellationToken cancellationToken)
    {
        var team = await db.Teams.FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);
        if (team is null)
        {
            return Results.NotFound();
        }

        var member = await db.TeamMembers.FirstOrDefaultAsync(
            m => m.TeamId == teamId && m.EmployeeProfileId == employeeProfileId,
            cancellationToken);
        if (member is null)
        {
            return Results.NotFound();
        }

        db.TeamMembers.Remove(member);
        team.ModifiedDate = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(await LoadTeamDetailAsync(db, teamId, cancellationToken));
    }

    private static async Task<IResult> HideProfileAsync(
        Guid teamId,
        TeamEmployeeRequest request,
        AdaptiveTeamBuilderDbContext db,
        CancellationToken cancellationToken)
    {
        var team = await db.Teams.FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);
        if (team is null)
        {
            return Results.NotFound();
        }

        var profileExists = await db.EmployeeProfiles
            .AnyAsync(p => p.Id == request.EmployeeProfileId, cancellationToken);
        if (!profileExists)
        {
            return Results.BadRequest(new { error = "Employee profile was not found." });
        }

        // Hide also removes them from the team if currently selected.
        var member = await db.TeamMembers.FirstOrDefaultAsync(
            m => m.TeamId == teamId && m.EmployeeProfileId == request.EmployeeProfileId,
            cancellationToken);
        if (member is not null)
        {
            db.TeamMembers.Remove(member);
        }

        var alreadyHidden = await db.TeamHiddenProfiles.AnyAsync(
            h => h.TeamId == teamId && h.EmployeeProfileId == request.EmployeeProfileId,
            cancellationToken);
        if (!alreadyHidden)
        {
            db.TeamHiddenProfiles.Add(new TeamHiddenProfile
            {
                TeamId = teamId,
                EmployeeProfileId = request.EmployeeProfileId,
                HiddenDate = DateTime.UtcNow,
            });
        }

        team.ModifiedDate = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(await LoadTeamDetailAsync(db, teamId, cancellationToken));
    }

    private static async Task<IResult> UnhideProfileAsync(
        Guid teamId,
        Guid employeeProfileId,
        AdaptiveTeamBuilderDbContext db,
        CancellationToken cancellationToken)
    {
        var team = await db.Teams.FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);
        if (team is null)
        {
            return Results.NotFound();
        }

        var hidden = await db.TeamHiddenProfiles.FirstOrDefaultAsync(
            h => h.TeamId == teamId && h.EmployeeProfileId == employeeProfileId,
            cancellationToken);
        if (hidden is null)
        {
            return Results.NotFound();
        }

        db.TeamHiddenProfiles.Remove(hidden);
        team.ModifiedDate = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(await LoadTeamDetailAsync(db, teamId, cancellationToken));
    }

    private static async Task<TeamDetailDto?> LoadTeamDetailAsync(
        AdaptiveTeamBuilderDbContext db,
        Guid teamId,
        CancellationToken cancellationToken)
    {
        var team = await db.Teams.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);
        if (team is null)
        {
            return null;
        }

        var positionTypes = await db.PositionTypes.AsNoTracking()
            .OrderBy(p => p.SortOrder)
            .ToListAsync(cancellationToken);

        var requirements = await db.TeamPositionRequirements.AsNoTracking()
            .Where(r => r.TeamId == teamId)
            .ToListAsync(cancellationToken);

        var members = await db.TeamMembers.AsNoTracking()
            .Where(m => m.TeamId == teamId)
            .Select(m => new
            {
                m.EmployeeProfileId,
                m.EmployeeProfile.FirstName,
                m.EmployeeProfile.LastName,
                m.EmployeeProfile.DisplayName,
                m.EmployeeProfile.Title,
                PositionTypeCode = m.EmployeeProfile.PositionType.Code,
                PositionTypeSort = m.EmployeeProfile.PositionType.SortOrder,
                LevelCode = m.EmployeeProfile.ExperienceLevel != null
                    ? m.EmployeeProfile.ExperienceLevel.Code
                    : null,
                LevelSort = m.EmployeeProfile.ExperienceLevel != null
                    ? m.EmployeeProfile.ExperienceLevel.SortOrder
                    : int.MaxValue,
                SpecialtyCode = m.EmployeeProfile.RoleSpecialty != null
                    ? m.EmployeeProfile.RoleSpecialty.Code
                    : null,
            })
            .ToListAsync(cancellationToken);

        var orderedMembers = members
            .OrderBy(m => m.PositionTypeSort)
            .ThenBy(m => m.LevelSort)
            .ThenBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .Select(m => new TeamMemberDto(
                m.EmployeeProfileId,
                m.FirstName,
                m.LastName,
                m.DisplayName,
                m.PositionTypeCode,
                m.SpecialtyCode,
                m.LevelCode,
                m.Title))
            .ToList();

        var selectedCounts = orderedMembers
            .GroupBy(m => m.PositionType)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var requirementDtos = positionTypes.Select(pt =>
        {
            var required = requirements.FirstOrDefault(r => r.PositionTypeId == pt.Id)?.RequiredCount ?? 0;
            selectedCounts.TryGetValue(pt.Code, out var selected);
            return new TeamRequirementDto(pt.Code, pt.Name, required, selected);
        }).ToList();

        var hiddenIds = await db.TeamHiddenProfiles.AsNoTracking()
            .Where(h => h.TeamId == teamId)
            .Select(h => h.EmployeeProfileId)
            .ToListAsync(cancellationToken);

        return new TeamDetailDto(
            team.Id,
            team.Name,
            team.ContractId,
            team.CreatedDate,
            team.ModifiedDate,
            requirementDtos,
            orderedMembers,
            hiddenIds);
    }

    private static string? NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var trimmed = name.Trim();
        return trimmed.Length <= 200 ? trimmed : trimmed[..200];
    }
}
