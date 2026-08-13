namespace AdaptiveTeamBuilder.Data.Entities;

public class ContractWorkMode
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}

public class ContractEngagementType
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}

public class ContractSkillPriority
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public ICollection<ContractSkill> ContractSkills { get; set; } = new List<ContractSkill>();
}

public class ContractConstraintType
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public ICollection<ContractConstraint> ContractConstraints { get; set; } = new List<ContractConstraint>();
}

public class ContractDeliveryRiskLevel
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public decimal ConfidenceFactor { get; set; }

    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}

public class ContractStrategicValueLevel
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}

public class Contract
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;

    public string OutcomeSummary { get; set; } = string.Empty;

    public string ScopeSummary { get; set; } = string.Empty;

    public int EngagementTypeId { get; set; }

    public int WorkModeId { get; set; }

    public int? DurationWeeks { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? TargetDeliveryDate { get; set; }

    public decimal EstimatedContractValue { get; set; }

    public decimal EstimatedProfit { get; set; }

    public decimal EstimatedMarginPercent { get; set; }

    public decimal WinProbabilityPercent { get; set; }

    public int DeliveryRiskId { get; set; }

    public int StrategicValueId { get; set; }

    public decimal StaffingFte { get; set; }

    public string? SpecialistStaffingNeeded { get; set; }

    public bool IsDefault { get; set; }

    /// <summary>Designed demo rotation sequence among never-selected contracts.</summary>
    public int DemoSortOrder { get; set; }

    /// <summary>Utc stamp when this contract was last chosen via control.select; drives list rotation.</summary>
    public DateTime? LastSelectedAt { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime ModifiedDate { get; set; }

    public ContractEngagementType EngagementType { get; set; } = null!;

    public ContractWorkMode WorkMode { get; set; } = null!;

    public ContractDeliveryRiskLevel DeliveryRisk { get; set; } = null!;

    public ContractStrategicValueLevel StrategicValue { get; set; } = null!;

    public ICollection<ContractSkill> Skills { get; set; } = new List<ContractSkill>();

    public ICollection<ContractConstraint> Constraints { get; set; } = new List<ContractConstraint>();

    public ICollection<ContractDeliverable> Deliverables { get; set; } = new List<ContractDeliverable>();

    public ICollection<ContractMilestone> Milestones { get; set; } = new List<ContractMilestone>();

    public ICollection<Team> Teams { get; set; } = new List<Team>();
}

public class ContractSkill
{
    public Guid ContractId { get; set; }

    public int SkillId { get; set; }

    public int PriorityId { get; set; }

    public Contract Contract { get; set; } = null!;

    public Skill Skill { get; set; } = null!;

    public ContractSkillPriority Priority { get; set; } = null!;
}

public class ContractConstraint
{
    public Guid ContractId { get; set; }

    public int ConstraintTypeId { get; set; }

    public Contract Contract { get; set; } = null!;

    public ContractConstraintType ConstraintType { get; set; } = null!;
}

public class ContractDeliverable
{
    public Guid Id { get; set; }

    public Guid ContractId { get; set; }

    public int SortOrder { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Detail { get; set; }

    public Contract Contract { get; set; } = null!;
}

public class ContractMilestone
{
    public Guid Id { get; set; }

    public Guid ContractId { get; set; }

    public int SortOrder { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateOnly? TargetDate { get; set; }

    public string? Description { get; set; }

    public Contract Contract { get; set; } = null!;
}
