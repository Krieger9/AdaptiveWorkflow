namespace AdaptiveTeamBuilder.Data.Entities;

public class PositionType
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public ICollection<EmployeeProfile> EmployeeProfiles { get; set; } = new List<EmployeeProfile>();

    public ICollection<RoleSpecialty> RoleSpecialties { get; set; } = new List<RoleSpecialty>();
}

public class ExperienceLevel
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public ICollection<EmployeeProfile> EmployeeProfiles { get; set; } = new List<EmployeeProfile>();
}

public class RoleSpecialty
{
    public int Id { get; set; }

    public int PositionTypeId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public PositionType PositionType { get; set; } = null!;

    public ICollection<EmployeeProfile> EmployeeProfiles { get; set; } = new List<EmployeeProfile>();
}

public class Skill
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<EmployeeProfileSkill> EmployeeProfileSkills { get; set; } = new List<EmployeeProfileSkill>();
}

public class EmployeeProfileSkill
{
    public Guid EmployeeProfileId { get; set; }

    public int SkillId { get; set; }

    public EmployeeProfile EmployeeProfile { get; set; } = null!;

    public Skill Skill { get; set; } = null!;
}

public class EmployeeProfile
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public int PositionTypeId { get; set; }

    public int? ExperienceLevelId { get; set; }

    public int? RoleSpecialtyId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Summary { get; set; }

    public int? YearsExperience { get; set; }

    public string? Location { get; set; }

    public string? Availability { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime ModifiedDate { get; set; }

    public PositionType PositionType { get; set; } = null!;

    public ExperienceLevel? ExperienceLevel { get; set; }

    public RoleSpecialty? RoleSpecialty { get; set; }

    public ICollection<EmployeeProfileSkill> EmployeeProfileSkills { get; set; } = new List<EmployeeProfileSkill>();
}
