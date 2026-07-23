namespace AdaptiveTeamBuilder.Data.Contracts;

public record CollaborationAppContextDto(
    string DomainDescription,
    int ContractCount,
    IReadOnlyList<string> DatasetSummaries);

public record CollaborationScreenContextDto(
    string ScreenId,
    string Title,
    IReadOnlyList<string> AvailableActions);

public record CollaborationControlSnapshotDto(
    string ControlId,
    string ControlType,
    string Label,
    bool Expanded,
    IReadOnlyDictionary<string, string> Data,
    IReadOnlyDictionary<string, string>? DetailData);

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
    IReadOnlyList<CollaborationInteractionEventDto> Events,
    CollaborationTendencyBundleDto Tendencies);

public record CollaborationSuggestionDto(
    string Id,
    string Kind,
    string Label,
    string? TargetControlId);

public record CollaborationAdviseResponse(
    string PromptPreview,
    CollaborationTendencyBundleDto UpdatedTendencies,
    IReadOnlyList<CollaborationSuggestionDto> Suggestions);

public record CollaborationTendenciesResponse(
    CollaborationTendencyBundleDto Tendencies);
