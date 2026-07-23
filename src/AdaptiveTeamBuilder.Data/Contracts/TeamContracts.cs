namespace AdaptiveTeamBuilder.Data.Contracts;

public record TeamListItemDto(Guid Id, string Name);

public record TeamRequirementDto(
    string PositionType,
    string PositionTypeName,
    int RequiredCount,
    int SelectedCount);

public record TeamMemberDto(
    Guid EmployeeProfileId,
    string FirstName,
    string LastName,
    string DisplayName,
    string PositionType,
    string? RoleSpecialty,
    string? Level,
    string Title);

public record TeamDetailDto(
    Guid Id,
    string Name,
    DateTime CreatedDate,
    DateTime ModifiedDate,
    IReadOnlyList<TeamRequirementDto> Requirements,
    IReadOnlyList<TeamMemberDto> Members,
    IReadOnlyList<Guid> HiddenEmployeeProfileIds);

public record CreateTeamRequest(string Name);

public record RenameTeamRequest(string Name);

public record UpsertTeamRequirementsRequest(
    IReadOnlyList<TeamRequirementInput> Requirements);

public record TeamRequirementInput(string PositionType, int RequiredCount);

public record TeamEmployeeRequest(Guid EmployeeProfileId);
