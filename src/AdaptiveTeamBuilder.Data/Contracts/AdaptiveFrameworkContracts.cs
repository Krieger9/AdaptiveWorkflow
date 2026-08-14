namespace AdaptiveTeamBuilder.Data.Contracts;

// API contracts for the adaptive UI preference framework.
// Vocabulary is locked (see docs/vocabulary.md): Surface, Surface Path, Interaction,
// Choice Set, Causation, Dimension, Belief, Conviction, Tenure, Revision, Suggestion,
// Probe, Glossary. Numeric machinery terms are deliberately absent.

public record AppContextDto(
    string DomainDescription,
    int ItemCount,
    IReadOnlyList<string> DatasetSummaries);

/// <summary>
/// Current UI presentation state visible on the surface.
/// </summary>
public record ViewStateDto(
    /// <summary>values | graph</summary>
    string SignalsDisplay,
    IReadOnlyList<string> ExpandedControlIds);

/// <summary>
/// Context of the surface (a region of UI that declares a purpose) an interaction batch
/// happened on. Assembled client-side from the surface registry by depth-first traversal.
/// </summary>
public record SurfaceContextDto(
    /// <summary>Ordered surface ids from root to leaf, e.g. ["page:contracts", "section:contracts.list"].</summary>
    IReadOnlyList<string> SurfacePath,
    string Title,
    IReadOnlyList<string> AvailableActions,
    ViewStateDto ViewState,
    /// <summary>Deterministic prose block generated from the surface tree (purpose + inherited domain).</summary>
    string? AssembledContext,
    /// <summary>Hash of <see cref="AssembledContext"/> so the harness can tell context changes from prompt changes.</summary>
    string? ContextHash,
    IReadOnlyDictionary<string, string>? Annotations);

public record ControlSnapshotDto(
    string ControlId,
    string ControlType,
    string Label,
    bool Expanded,
    IReadOnlyDictionary<string, string> Data,
    IReadOnlyDictionary<string, string>? DetailData,
    IReadOnlyDictionary<string, string>? Annotations);

/// <summary>Attribute snapshot of the entity an interaction targeted.</summary>
public record EntityRefDto(
    string Type,
    string Id,
    IReadOnlyDictionary<string, string> Attrs);

/// <summary>One alternative that was visible and available at the moment of an interaction.</summary>
public record ChoiceSetItemDto(
    string Id,
    IReadOnlyDictionary<string, string> Attrs);

/// <summary>
/// One logged user or system act against a surface. The atomic unit of evidence.
/// </summary>
public record InteractionDto(
    string Id,
    DateTime At,
    string SessionId,
    /// <summary>Monotonic sequence within the session.</summary>
    int Seq,
    /// <summary>Ordered surface ids from root to leaf.</summary>
    IReadOnlyList<string> SurfacePath,
    /// <summary>Semantic action, e.g. control.expand | control.collapse | view.change | control.select.</summary>
    string Action,
    string? ControlId,
    string? Label,
    string? ValueBefore,
    string? ValueAfter,
    /// <summary>user | system-default | restored | agent-applied.</summary>
    string Causation,
    /// <summary>Set server-side when a user act undoes a recent agent-applied state.</summary>
    bool? Reversal,
    EntityRefDto? Entity,
    /// <summary>The alternatives visible at the moment of the interaction. Enables negatives.</summary>
    IReadOnlyList<ChoiceSetItemDto>? ChoiceSet,
    IReadOnlyDictionary<string, string>? Meta);

/// <summary>
/// The markdown belief document for one tier, plus recent decision-turn digests.
/// The document holds beliefs (per surface scope and dimension), each with
/// Conviction and Tenure, and a changelog of revisions and challenges-that-held.
/// </summary>
public record BeliefProfileDto(
    /// <summary>control | application | universal.</summary>
    string Tier,
    /// <summary>The full markdown belief document.</summary>
    string Document,
    /// <summary>app | stub | llm.</summary>
    string Source,
    int Version,
    DateTime? UpdatedAt,
    /// <summary>Newest-last compact digests of recent decision turns (max ~5).</summary>
    IReadOnlyList<string>? RecentTurnDigests = null);

/// <summary>
/// A user-facing offer to change UI behavior based on a belief.
/// </summary>
public record SuggestionDto(
    string Id,
    /// <summary>expand | collapse | select | set-view</summary>
    string Kind,
    string Label,
    string? TargetControlId,
    IReadOnlyDictionary<string, string>? Payload,
    /// <summary>Preference dimension the suggestion draws on, when known.</summary>
    string? Dimension = null,
    /// <summary>True when issued to resolve agent uncertainty rather than because conviction is high.</summary>
    bool IsProbe = false,
    /// <summary>Agent-written rationale, drawn from the belief's "What I'm leaning on".</summary>
    string? Rationale = null);

/// <summary>
/// Durable layout the client should apply from belief interpretation (cold-start bootstrap).
/// </summary>
public record PreferredLayoutDto(
    bool ExpandAll,
    /// <summary>values | graph | null to leave the current/default display.</summary>
    string? SignalsDisplay,
    string? Rationale,
    /// <summary>
    /// When ExpandAll is false, expand this many highest-ranked cards by ExpandBySignal
    /// (e.g. 2 for keep-top-two-by-Margin). Null means leave all collapsed.
    /// </summary>
    int? ExpandTopCount = null,
    /// <summary>
    /// Commercial signal used with ExpandTopCount: Margin | Profit | Value | Win prob.
    /// (or estimatedMarginPercent / estimatedProfit / estimatedContractValue / winProbabilityPercent).
    /// </summary>
    string? ExpandBySignal = null);

public record AdviseRequest(
    AppContextDto App,
    SurfaceContextDto Surface,
    IReadOnlyList<ControlSnapshotDto> Controls,
    IReadOnlyList<InteractionDto> Interactions);

public record AdviseResponse(
    string PromptPreview,
    IReadOnlyList<SuggestionDto> Suggestions,
    PreferredLayoutDto? PreferredLayout = null);

public record ProfileResponse(BeliefProfileDto Profile);

public record ObservationsRequest(
    Guid UserId,
    string SessionId,
    AppContextDto App,
    SurfaceContextDto Surface,
    IReadOnlyList<ControlSnapshotDto> Controls,
    IReadOnlyList<InteractionDto> Interactions);

public record ObservationsResponse(
    Guid UserId,
    int AcceptedInteractionCount,
    string Status,
    string PromptPreview,
    IReadOnlyList<SuggestionDto> Suggestions,
    PreferredLayoutDto? PreferredLayout = null);
