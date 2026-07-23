namespace AdaptiveTeamBuilder.Data.Entities;

public class Team
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }

    public DateTime ModifiedDate { get; set; }

    public ICollection<TeamPositionRequirement> PositionRequirements { get; set; } = new List<TeamPositionRequirement>();

    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();

    public ICollection<TeamHiddenProfile> HiddenProfiles { get; set; } = new List<TeamHiddenProfile>();
}

public class TeamPositionRequirement
{
    public Guid TeamId { get; set; }

    public int PositionTypeId { get; set; }

    public int RequiredCount { get; set; }

    public Team Team { get; set; } = null!;

    public PositionType PositionType { get; set; } = null!;
}

public class TeamMember
{
    public Guid TeamId { get; set; }

    public Guid EmployeeProfileId { get; set; }

    public DateTime AddedDate { get; set; }

    public Team Team { get; set; } = null!;

    public EmployeeProfile EmployeeProfile { get; set; } = null!;
}

public class TeamHiddenProfile
{
    public Guid TeamId { get; set; }

    public Guid EmployeeProfileId { get; set; }

    public DateTime HiddenDate { get; set; }

    public Team Team { get; set; } = null!;

    public EmployeeProfile EmployeeProfile { get; set; } = null!;
}
