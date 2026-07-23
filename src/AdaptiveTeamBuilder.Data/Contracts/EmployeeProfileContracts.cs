namespace AdaptiveTeamBuilder.Data.Contracts;

public record EmployeeProfileDto(
    Guid Id,
    string FirstName,
    string LastName,
    string DisplayName,
    string PositionType,
    string? RoleSpecialty,
    string? Level,
    string Title,
    string? Summary,
    IReadOnlyList<string> Skills,
    int? YearsExperience,
    string? Location,
    string? Availability,
    DateTime CreatedDate,
    DateTime ModifiedDate);

public record EmployeeProfileListItemDto(
    Guid Id,
    string DisplayName,
    string PositionType,
    string? RoleSpecialty,
    string? Level,
    string Title,
    string? Location,
    string? Availability);
