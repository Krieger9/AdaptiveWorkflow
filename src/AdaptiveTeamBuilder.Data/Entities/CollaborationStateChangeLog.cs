namespace AdaptiveTeamBuilder.Data.Entities;

/// <summary>
/// Records the AI's stated reason each time it changes a user's collaboration state,
/// so the reasoning behind profile updates can be inspected. Optionally linked to the
/// decision-turn digest that triggered the change.
/// </summary>
public class CollaborationStateChangeLog
{
    public long Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>Turn digest that prompted the change, when available.</summary>
    public long? TurnDigestId { get; set; }

    /// <summary>Concise natural-language reason the AI gave for changing the profile.</summary>
    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }

    public CollaborationTurnDigest? TurnDigest { get; set; }
}
