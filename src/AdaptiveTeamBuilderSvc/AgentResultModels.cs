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

    /// <summary>When ExpandAll is false, expand this many top-ranked cards by ExpandBySignal.</summary>
    public int? ExpandTopCount { get; set; }

    /// <summary>Margin | Profit | Value | Win | estimatedMarginPercent | …</summary>
    public string? ExpandBySignal { get; set; }
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
        var expandBySignal = NormalizeExpandBySignal(layout.ExpandBySignal);
        var expandTopCount = NormalizeExpandTopCount(layout.ExpandTopCount);

        if (expandBySignal is not null && expandTopCount is null)
        {
            expandTopCount = 2;
        }

        if (expandTopCount is not null && expandBySignal is null)
        {
            expandTopCount = null;
        }

        // Signal-driven top-N wins over expand-all when both are present.
        var expandAll = layout.ExpandAll && expandTopCount is null;

        return new CollaborationPreferredLayoutDto(
            expandAll,
            display,
            string.IsNullOrWhiteSpace(layout.Rationale) ? null : layout.Rationale.Trim(),
            expandTopCount,
            expandBySignal);
    }

    internal static string? NormalizeSignalsDisplay(string? value)
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

    internal static string? NormalizeExpandBySignal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var key = value.Trim().ToLowerInvariant()
            .Replace('_', ' ')
            .Replace('-', ' ');

        if (key is "margin" or "estimatedmarginpercent" or "estimated margin"
            or "estimated margin percent" or "margin percent" or "margin%")
        {
            return "Margin";
        }

        if (key is "profit" or "estimatedprofit" or "estimated profit")
        {
            return "Profit";
        }

        if (key is "value" or "estimatedcontractvalue" or "contract value" or "estimated value")
        {
            return "Value";
        }

        if (key is "win" or "win prob" or "win prob." or "winprobability"
            or "winprobabilitypercent" or "win probability" or "win probability percent")
        {
            return "Win prob.";
        }

        return null;
    }

    internal static int? NormalizeExpandTopCount(int? count)
    {
        if (count is null or <= 0)
        {
            return null;
        }

        // Cap to a sensible subset; expand-all remains the path for opening everything.
        return Math.Clamp(count.Value, 1, 8);
    }
}
