namespace AdaptiveTeamBuilder.Data.Entities;

/// <summary>
/// The markdown belief document the agent maintains via read-modify-write for one tier.
/// Replaces the former prose-only <c>UserCollaborationStates.TendencyProse</c> storage.
/// Every accepted write bumps <see cref="Version"/>; versioned copies are archived to
/// <c>data/profiles/{userId}/{tier}.v{n}.md</c> on disk.
/// </summary>
public class BeliefDocument
{
    public Guid UserId { get; set; }

    /// <summary>Profile tier: control | application | universal.</summary>
    public string Tier { get; set; } = "control";

    /// <summary>The full markdown belief document.</summary>
    public string Document { get; set; } = string.Empty;

    /// <summary>Source of the latest write: app | stub | llm.</summary>
    public string Source { get; set; } = "app";

    public int Version { get; set; }

    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
}
