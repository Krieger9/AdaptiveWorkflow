namespace AdaptiveTeamBuilder.Data.Entities;

/// <summary>
/// Unified log of belief-document changes: revisions AND challenges-that-held.
/// Replaces the former <c>CollaborationStateChangeLogs</c> table with the framework's
/// vocabulary. Optionally linked to the decision-turn digest that triggered the entry.
/// </summary>
public class Revision
{
    public long Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>Surface scope the revised/challenged belief applies to.</summary>
    public string SurfacePath { get; set; } = string.Empty;

    /// <summary>revised | challenged-held | created | retired | proposed.</summary>
    public string Kind { get; set; } = "revised";

    /// <summary>The agent's stated reasoning for the change (or for holding).</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Turn digest that prompted the entry, when available.</summary>
    public long? TurnDigestId { get; set; }

    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }

    public TurnDigest? TurnDigest { get; set; }
}
