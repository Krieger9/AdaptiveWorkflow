namespace AdaptiveTeamBuilder.Data.Entities;

public class UserCollaborationState
{
    public Guid UserId { get; set; }

    /// <summary>Prose override of app-default collaboration tendencies.</summary>
    public string? TendencyProse { get; set; }

    /// <summary>Source of the override: stub | llm (app defaults live in code).</summary>
    public string TendencySource { get; set; } = "stub";

    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
}
