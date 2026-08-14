namespace AdaptiveTeamBuilder.Data.Entities;

/// <summary>
/// One logged user or system act against a surface. The atomic unit of evidence.
/// Mirrors the append-only JSONL interaction log so evidence can be queried directly.
/// </summary>
public class Interaction
{
    public long Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>Client-generated session identifier, e.g. <c>s_20260814t1802_ab12</c>.</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>Client-assigned interaction id (stable across DB and JSONL log).</summary>
    public string ClientInteractionId { get; set; } = string.Empty;

    /// <summary>Monotonic sequence within the session.</summary>
    public int Seq { get; set; }

    public DateTime At { get; set; }

    /// <summary>
    /// Ordered surface ids from root to leaf joined with " / ",
    /// e.g. <c>page:contracts / section:contracts.list / group:contract.card</c>.
    /// </summary>
    public string SurfacePath { get; set; } = string.Empty;

    public string? ControlId { get; set; }

    /// <summary>Semantic action, e.g. control.expand, view.change, control.select.</summary>
    public string Action { get; set; } = string.Empty;

    public string? ValueBefore { get; set; }

    public string? ValueAfter { get; set; }

    /// <summary>Why the state change happened: user | system-default | restored | agent-applied.</summary>
    public string Causation { get; set; } = "user";

    /// <summary>True when a user act undid a recent agent-applied state.</summary>
    public bool Reversal { get; set; }

    /// <summary>JSON snapshot of the targeted entity ({ type, id, attrs }).</summary>
    public string? EntityJson { get; set; }

    /// <summary>JSON array of the visible alternatives at the moment of the interaction.</summary>
    public string? ChoiceSetJson { get; set; }

    /// <summary>JSON dictionary of additional semantic metadata.</summary>
    public string? MetaJson { get; set; }

    public User? User { get; set; }
}
