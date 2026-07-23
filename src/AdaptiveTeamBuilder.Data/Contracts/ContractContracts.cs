namespace AdaptiveTeamBuilder.Data.Contracts;

public record ContractListItemDto(
    Guid Id,
    string Code,
    string Title,
    string ClientName,
    string OutcomeSummary,
    string EngagementType,
    string EngagementTypeName,
    string WorkMode,
    string WorkModeName,
    int? DurationWeeks,
    DateOnly? TargetDeliveryDate,
    decimal EstimatedContractValue,
    decimal EstimatedProfit,
    decimal EstimatedMarginPercent,
    decimal WinProbabilityPercent,
    string DeliveryRisk,
    string DeliveryRiskName,
    string StrategicValue,
    string StrategicValueName,
    decimal StaffingFte,
    string? SpecialistStaffingNeeded,
    decimal ExpectedProfit,
    decimal RiskAdjustedProfit,
    decimal? ProfitPerMonth,
    decimal ProfitPerFte,
    int TeamCount);

public record ContractSkillDto(string Name, string Priority, string PriorityName);

public record ContractConstraintDto(string Code, string Name);

public record ContractDeliverableDto(Guid Id, int SortOrder, string Title, string? Detail);

public record ContractMilestoneDto(
    Guid Id,
    int SortOrder,
    string Name,
    DateOnly? TargetDate,
    string? Description);

public record ContractDetailDto(
    Guid Id,
    string Code,
    string Title,
    string ClientName,
    string OutcomeSummary,
    string ScopeSummary,
    string EngagementType,
    string EngagementTypeName,
    string WorkMode,
    string WorkModeName,
    int? DurationWeeks,
    DateOnly? StartDate,
    DateOnly? TargetDeliveryDate,
    decimal EstimatedContractValue,
    decimal EstimatedProfit,
    decimal EstimatedMarginPercent,
    decimal WinProbabilityPercent,
    string DeliveryRisk,
    string DeliveryRiskName,
    string StrategicValue,
    string StrategicValueName,
    decimal StaffingFte,
    string? SpecialistStaffingNeeded,
    decimal ExpectedProfit,
    decimal RiskAdjustedProfit,
    decimal? ProfitPerMonth,
    decimal ProfitPerFte,
    bool IsDefault,
    IReadOnlyList<ContractSkillDto> Skills,
    IReadOnlyList<ContractConstraintDto> Constraints,
    IReadOnlyList<ContractDeliverableDto> Deliverables,
    IReadOnlyList<ContractMilestoneDto> Milestones);

public static class ContractSignalMath
{
    private const decimal WeeksPerMonth = 4.345m;

    public static decimal ExpectedProfit(decimal estimatedProfit, decimal winProbabilityPercent) =>
        RoundMoney(estimatedProfit * (winProbabilityPercent / 100m));

    public static decimal RiskAdjustedProfit(
        decimal estimatedProfit,
        decimal winProbabilityPercent,
        decimal confidenceFactor) =>
        RoundMoney(estimatedProfit * (winProbabilityPercent / 100m) * confidenceFactor);

    public static decimal? ProfitPerMonth(decimal estimatedProfit, int? durationWeeks)
    {
        if (durationWeeks is null or <= 0)
        {
            return null;
        }

        return RoundMoney(estimatedProfit / (durationWeeks.Value / WeeksPerMonth));
    }

    public static decimal ProfitPerFte(decimal estimatedProfit, decimal staffingFte) =>
        staffingFte <= 0 ? 0 : RoundMoney(estimatedProfit / staffingFte);

    private static decimal RoundMoney(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
