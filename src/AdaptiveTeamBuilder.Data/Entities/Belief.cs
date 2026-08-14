namespace AdaptiveTeamBuilder.Data.Entities;

/// <summary>
/// The agent's current position on one dimension within one surface scope.
/// Rows are projections parsed from the belief document; the markdown document
/// remains the medium the agent reads and writes.
/// </summary>
public class Belief
{
    public long Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>Surface scope the belief applies to, e.g. <c>section:contracts.list</c>.</summary>
    public string SurfacePath { get; set; } = string.Empty;

    /// <summary>Preference dimension id, e.g. <c>disclosure-default</c>.</summary>
    public string Dimension { get; set; } = string.Empty;

    /// <summary>The belief statement in prose.</summary>
    public string Statement { get; set; } = string.Empty;

    /// <summary>noticed | tentative | working theory | settled | entrenched.</summary>
    public string Conviction { get; set; } = "noticed";

    /// <summary>How long the belief has been held and how many challenges it survived, in prose.</summary>
    public string Tenure { get; set; } = string.Empty;

    /// <summary>Evidence prose ("What I'm leaning on").</summary>
    public string LeaningOn { get; set; } = string.Empty;

    /// <summary>Falsifier prose ("What would change my mind").</summary>
    public string ChangeCriteria { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
}
