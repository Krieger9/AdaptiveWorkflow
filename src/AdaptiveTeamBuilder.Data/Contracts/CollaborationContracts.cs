namespace AdaptiveTeamBuilder.Data.Contracts;

public record CollaborationAppContextDto(
    string DomainDescription,
    int ContractCount,
    IReadOnlyList<string> DatasetSummaries);

/// <summary>
/// Current UI presentation preferences visible on the screen.
/// </summary>
public record CollaborationViewStateDto(
    /// <summary>values | graph</summary>
    string SignalsDisplay,
    IReadOnlyList<string> ExpandedControlIds);

public record CollaborationScreenContextDto(
    string ScreenId,
    string Title,
    IReadOnlyList<string> AvailableActions,
    CollaborationViewStateDto ViewState,
    IReadOnlyDictionary<string, string>? Annotations);

public record CollaborationControlSnapshotDto(
    string ControlId,
    string ControlType,
    string Label,
    bool Expanded,
    IReadOnlyDictionary<string, string> Data,
    IReadOnlyDictionary<string, string>? DetailData,
    IReadOnlyDictionary<string, string>? Annotations);

public record CollaborationInteractionEventDto(
    DateTime At,
    string ScreenId,
    string Type,
    string? ControlId,
    string? Label,
    IReadOnlyDictionary<string, string>? Meta);

public record CollaborationTendencyBundleDto(
    string AppDefaults,
    string? UserOverride,
    DateTime? UpdatedAt,
    string Source);

public record CollaborationAdviseRequest(
    CollaborationAppContextDto App,
    CollaborationScreenContextDto Screen,
    IReadOnlyList<CollaborationControlSnapshotDto> Controls,
    IReadOnlyList<CollaborationInteractionEventDto> Events);

public record CollaborationSuggestionDto(
    string Id,
    /// <summary>expand | collapse | select | set-view</summary>
    string Kind,
    string Label,
    string? TargetControlId,
    IReadOnlyDictionary<string, string>? Payload);

/// <summary>
/// Durable layout the client should apply from profile interpretation (cold-start bootstrap).
/// </summary>
public record CollaborationPreferredLayoutDto(
    bool ExpandAll,
    /// <summary>values | graph | null to leave the current/default display.</summary>
    string? SignalsDisplay,
    string? Rationale);

public record CollaborationAdviseResponse(
    string PromptPreview,
    IReadOnlyList<CollaborationSuggestionDto> Suggestions,
    CollaborationPreferredLayoutDto? PreferredLayout = null);

public record CollaborationProfileResponse(
    CollaborationTendencyBundleDto Tendencies);

public record CollaborationObservationsRequest(
    Guid UserId,
    CollaborationAppContextDto App,
    CollaborationScreenContextDto Screen,
    IReadOnlyList<CollaborationControlSnapshotDto> Controls,
    IReadOnlyList<CollaborationInteractionEventDto> Events);

public record CollaborationObservationsResponse(
    Guid UserId,
    int AcceptedEventCount,
    string Status,
    string PromptPreview,
    IReadOnlyList<CollaborationSuggestionDto> Suggestions,
    CollaborationPreferredLayoutDto? PreferredLayout = null);
