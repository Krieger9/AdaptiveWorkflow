namespace AdaptiveTeamBuilder.Data.Entities;

public class UserCollaborationState
{
    public Guid UserId { get; set; }

    /// <summary>Prose override of app-default collaboration tendencies.</summary>
    public string? TendencyProse { get; set; }

    /// <summary>Source of the override: stub | llm (app defaults live in code).</summary>
    public string TendencySource { get; set; } = "stub";

    /// <summary>
    /// JSON array of compact decision-turn digests (newest last), used to detect habit shifts
    /// across ~4-5 visits without stuffing the durable TendencyProse.
    /// </summary>
    public string? RecentTurnDigestsJson { get; set; }

    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
}
