namespace AdaptiveTeamBuilder.Data.Entities;

public class User
{
    public Guid Id { get; set; }

    /// <summary>Entra ID object id (oid claim).</summary>
    public string AzureAdObjectId { get; set; } = string.Empty;

    /// <summary>Preferred username / UPN from the Entra token.</summary>
    public string UserName { get; set; } = string.Empty;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? DisplayName { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime ModifiedDate { get; set; }

    public DateTime? LastLoggedInDate { get; set; }
}
