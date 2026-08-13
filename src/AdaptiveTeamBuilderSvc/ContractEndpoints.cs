using AdaptiveTeamBuilder.Data;
using AdaptiveTeamBuilder.Data.Contracts;
using Microsoft.EntityFrameworkCore;

namespace AdaptiveTeamBuilderSvc;

public static class ContractEndpoints
{
    public static RouteGroupBuilder MapContractEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/contracts")
            .WithTags("Contracts")
            .RequireAuthorization("AccessAsUser");

        group.MapGet("/", ListContractsAsync);
        group.MapGet("/default", GetDefaultContractAsync);
        group.MapGet("/{contractId:guid}", GetContractAsync);

        return group;
    }

    private static async Task<IResult> ListContractsAsync(
        AdaptiveTeamBuilderDbContext db,
        bool? all,
        CancellationToken cancellationToken)
    {
        // Default: least-recently-selected 3 (null LastSelectedAt first, then DemoSortOrder).
        // ?all=true returns the full pool for verifying seed/ranks.
        var query = db.Contracts.AsNoTracking()
            .OrderBy(c => c.LastSelectedAt == null ? 0 : 1)
            .ThenBy(c => c.LastSelectedAt)
            .ThenBy(c => c.DemoSortOrder)
            .ThenBy(c => c.ClientName)
            .ThenBy(c => c.Title)
            .Select(c => new
            {
                c.Id,
                c.Code,
                c.Title,
                c.ClientName,
                c.OutcomeSummary,
                EngagementType = c.EngagementType.Code,
                EngagementTypeName = c.EngagementType.Name,
                WorkMode = c.WorkMode.Code,
                WorkModeName = c.WorkMode.Name,
                c.DurationWeeks,
                c.TargetDeliveryDate,
                c.EstimatedContractValue,
                c.EstimatedProfit,
                c.EstimatedMarginPercent,
                c.WinProbabilityPercent,
                DeliveryRisk = c.DeliveryRisk.Code,
                DeliveryRiskName = c.DeliveryRisk.Name,
                DeliveryConfidence = c.DeliveryRisk.ConfidenceFactor,
                StrategicValue = c.StrategicValue.Code,
                StrategicValueName = c.StrategicValue.Name,
                c.StaffingFte,
                c.SpecialistStaffingNeeded,
                TeamCount = c.Teams.Count,
            });

        if (all != true)
        {
            query = query.Take(3);
        }

        var contracts = await query.ToListAsync(cancellationToken);

        var items = contracts.Select(c => new ContractListItemDto(
            c.Id,
            c.Code,
            c.Title,
            c.ClientName,
            c.OutcomeSummary,
            c.EngagementType,
            c.EngagementTypeName,
            c.WorkMode,
            c.WorkModeName,
            c.DurationWeeks,
            c.TargetDeliveryDate,
            c.EstimatedContractValue,
            c.EstimatedProfit,
            c.EstimatedMarginPercent,
            c.WinProbabilityPercent,
            c.DeliveryRisk,
            c.DeliveryRiskName,
            c.StrategicValue,
            c.StrategicValueName,
            c.StaffingFte,
            c.SpecialistStaffingNeeded,
            ContractSignalMath.ExpectedProfit(c.EstimatedProfit, c.WinProbabilityPercent),
            ContractSignalMath.RiskAdjustedProfit(
                c.EstimatedProfit,
                c.WinProbabilityPercent,
                c.DeliveryConfidence),
            ContractSignalMath.ProfitPerMonth(c.EstimatedProfit, c.DurationWeeks),
            ContractSignalMath.ProfitPerFte(c.EstimatedProfit, c.StaffingFte),
            c.TeamCount)).ToList();

        return Results.Ok(items);
    }

    private static async Task<IResult> GetDefaultContractAsync(
        AdaptiveTeamBuilderDbContext db,
        CancellationToken cancellationToken)
    {
        var detail = await ContractQuery.LoadDefaultAsync(db, cancellationToken);
        return detail is null ? Results.NotFound() : Results.Ok(detail);
    }

    private static async Task<IResult> GetContractAsync(
        Guid contractId,
        AdaptiveTeamBuilderDbContext db,
        CancellationToken cancellationToken)
    {
        var detail = await ContractQuery.LoadByIdAsync(db, contractId, cancellationToken);
        return detail is null ? Results.NotFound() : Results.Ok(detail);
    }
}

internal static class ContractQuery
{
    public static async Task<ContractDetailDto?> LoadDefaultAsync(
        AdaptiveTeamBuilderDbContext db,
        CancellationToken cancellationToken)
    {
        var id = await db.Contracts.AsNoTracking()
            .Where(c => c.IsDefault)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return id is null ? null : await LoadByIdAsync(db, id.Value, cancellationToken);
    }

    public static async Task<ContractDetailDto?> LoadByIdAsync(
        AdaptiveTeamBuilderDbContext db,
        Guid contractId,
        CancellationToken cancellationToken)
    {
        var contract = await db.Contracts.AsNoTracking()
            .Where(c => c.Id == contractId)
            .Select(c => new
            {
                c.Id,
                c.Code,
                c.Title,
                c.ClientName,
                c.OutcomeSummary,
                c.ScopeSummary,
                EngagementType = c.EngagementType.Code,
                EngagementTypeName = c.EngagementType.Name,
                WorkMode = c.WorkMode.Code,
                WorkModeName = c.WorkMode.Name,
                c.DurationWeeks,
                c.StartDate,
                c.TargetDeliveryDate,
                c.EstimatedContractValue,
                c.EstimatedProfit,
                c.EstimatedMarginPercent,
                c.WinProbabilityPercent,
                DeliveryRisk = c.DeliveryRisk.Code,
                DeliveryRiskName = c.DeliveryRisk.Name,
                DeliveryConfidence = c.DeliveryRisk.ConfidenceFactor,
                StrategicValue = c.StrategicValue.Code,
                StrategicValueName = c.StrategicValue.Name,
                c.StaffingFte,
                c.SpecialistStaffingNeeded,
                c.IsDefault,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (contract is null)
        {
            return null;
        }

        var skills = await db.ContractSkills.AsNoTracking()
            .Where(s => s.ContractId == contractId)
            .OrderBy(s => s.Priority.SortOrder)
            .ThenBy(s => s.Skill.Name)
            .Select(s => new ContractSkillDto(s.Skill.Name, s.Priority.Code, s.Priority.Name))
            .ToListAsync(cancellationToken);

        var constraints = await db.ContractConstraints.AsNoTracking()
            .Where(c => c.ContractId == contractId)
            .OrderBy(c => c.ConstraintType.SortOrder)
            .Select(c => new ContractConstraintDto(c.ConstraintType.Code, c.ConstraintType.Name))
            .ToListAsync(cancellationToken);

        var deliverables = await db.ContractDeliverables.AsNoTracking()
            .Where(d => d.ContractId == contractId)
            .OrderBy(d => d.SortOrder)
            .Select(d => new ContractDeliverableDto(d.Id, d.SortOrder, d.Title, d.Detail))
            .ToListAsync(cancellationToken);

        var milestones = await db.ContractMilestones.AsNoTracking()
            .Where(m => m.ContractId == contractId)
            .OrderBy(m => m.SortOrder)
            .Select(m => new ContractMilestoneDto(m.Id, m.SortOrder, m.Name, m.TargetDate, m.Description))
            .ToListAsync(cancellationToken);

        return new ContractDetailDto(
            contract.Id,
            contract.Code,
            contract.Title,
            contract.ClientName,
            contract.OutcomeSummary,
            contract.ScopeSummary,
            contract.EngagementType,
            contract.EngagementTypeName,
            contract.WorkMode,
            contract.WorkModeName,
            contract.DurationWeeks,
            contract.StartDate,
            contract.TargetDeliveryDate,
            contract.EstimatedContractValue,
            contract.EstimatedProfit,
            contract.EstimatedMarginPercent,
            contract.WinProbabilityPercent,
            contract.DeliveryRisk,
            contract.DeliveryRiskName,
            contract.StrategicValue,
            contract.StrategicValueName,
            contract.StaffingFte,
            contract.SpecialistStaffingNeeded,
            ContractSignalMath.ExpectedProfit(contract.EstimatedProfit, contract.WinProbabilityPercent),
            ContractSignalMath.RiskAdjustedProfit(
                contract.EstimatedProfit,
                contract.WinProbabilityPercent,
                contract.DeliveryConfidence),
            ContractSignalMath.ProfitPerMonth(contract.EstimatedProfit, contract.DurationWeeks),
            ContractSignalMath.ProfitPerFte(contract.EstimatedProfit, contract.StaffingFte),
            contract.IsDefault,
            skills,
            constraints,
            deliverables,
            milestones);
    }
}
