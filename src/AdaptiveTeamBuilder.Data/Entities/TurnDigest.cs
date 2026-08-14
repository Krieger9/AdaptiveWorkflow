namespace AdaptiveTeamBuilder.Data.Entities;

/// <summary>
/// A single normalized decision-turn digest row in the rolling recent-interaction log.
/// Rows are retained (not trimmed); the store reads only the newest N for prompt context.
/// </summary>
public class TurnDigest
{
    public long Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>Surface scope the turn happened on, e.g. <c>page:contracts / section:contracts.list</c>.</summary>
    public string SurfacePath { get; set; } = string.Empty;

    /// <summary>Per-user monotonic turn number (oldest = lowest).</summary>
    public int Sequence { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Compact single-line digest of the decision turn.</summary>
    public string DigestText { get; set; } = string.Empty;

    public User? User { get; set; }
}
