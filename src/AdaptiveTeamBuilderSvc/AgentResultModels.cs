using AdaptiveTeamBuilder.Data.Contracts;

namespace AdaptiveTeamBuilderSvc;

/// <summary>Structured output from the CollaborationAdvisor agent.</summary>
public sealed class AdviseAgentResult
{
    public AdvisePreferredLayoutResult? PreferredLayout { get; set; }

    public List<AdviseAgentSuggestion> Suggestions { get; set; } = [];
}

public sealed class AdvisePreferredLayoutResult
{
    public bool ExpandAll { get; set; }

    /// <summary>values | graph | omit/null to leave default.</summary>
    public string? SignalsDisplay { get; set; }

    public string? Rationale { get; set; }
}

public sealed class AdviseAgentSuggestion
{
    public string Id { get; set; } = string.Empty;

    /// <summary>expand | collapse | select | set-view</summary>
    public string Kind { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string? TargetControlId { get; set; }

    public Dictionary<string, string>? Payload { get; set; }
}

/// <summary>Structured output from the CollaborationProfileUpdater agent.</summary>
public sealed class ProfileUpdateAgentResult
{
    public string TendencyProse { get; set; } = string.Empty;
}

public static class AdviseAgentResultMapper
{
    public static IReadOnlyList<CollaborationSuggestionDto> ToSuggestions(
        AdviseAgentResult? result)
    {
        if (result?.Suggestions is not { Count: > 0 })
        {
            return [];
        }

        return result.Suggestions
            .Where(s => !string.IsNullOrWhiteSpace(s.Kind) && !string.IsNullOrWhiteSpace(s.Label))
            .Select(s => new CollaborationSuggestionDto(
                string.IsNullOrWhiteSpace(s.Id) ? Guid.NewGuid().ToString("N") : s.Id,
                s.Kind.Trim(),
                s.Label.Trim(),
                string.IsNullOrWhiteSpace(s.TargetControlId) ? null : s.TargetControlId,
                s.Payload is { Count: > 0 } ? s.Payload : null))
            .ToList();
    }

    public static CollaborationPreferredLayoutDto? ToPreferredLayout(AdviseAgentResult? result)
    {
        if (result?.PreferredLayout is null)
        {
            return null;
        }

        var layout = result.PreferredLayout;
        var display = NormalizeSignalsDisplay(layout.SignalsDisplay);
        return new CollaborationPreferredLayoutDto(
            layout.ExpandAll,
            display,
            string.IsNullOrWhiteSpace(layout.Rationale) ? null : layout.Rationale.Trim());
    }

    private static string? NormalizeSignalsDisplay(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "values" or "numeric" => "values",
            "graph" or "graphs" => "graph",
            _ => null,
        };
    }
}
