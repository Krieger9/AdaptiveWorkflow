namespace AdaptiveTeamBuilder.Data.Contracts;

public record UserDto(
    Guid Id,
    string AzureAdObjectId,
    string UserName,
    string? FirstName,
    string? LastName,
    string? DisplayName,
    DateTime CreatedDate,
    DateTime ModifiedDate,
    DateTime? LastLoggedInDate);

public record UpdateUserProfileRequest(
    string? FirstName,
    string? LastName,
    string? DisplayName);
