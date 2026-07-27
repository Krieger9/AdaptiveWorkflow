using System.Text;
using AdaptiveTeamBuilder.Data.Contracts;

namespace AdaptiveTeamBuilderSvc;

public interface ICollaborationAdvisor
{
    Task<CollaborationAdviseResponse> AdviseAsync(
        CollaborationAdviseRequest request,
        CollaborationTendencyBundleDto profile,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Heuristic advisor used when Foundry is not configured, and as a failure fallback.
/// Builds a preference-oriented prompt preview and applyable suggestions.
/// Does not update the user profile.
/// </summary>
public sealed class StubCollaborationAdvisor(
    ICollaborationAgentTranscriptLogger transcripts) : ICollaborationAdvisor
{
    public async Task<CollaborationAdviseResponse> AdviseAsync(
        CollaborationAdviseRequest request,
        CollaborationTendencyBundleDto profile,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = Advise(request, profile);
        await transcripts.WriteAsync(
            new CollaborationAgentTranscript
            {
                Agent = FoundryCollaborationAgents.AdvisorAgentName,
                Source = "stub",
                Prompt = response.PromptPreview,
                RetrievedProfile = profile,
                TurnContext = new CollaborationProfileUpdateContext(
                    request.Screen.ScreenId,
                    request.Screen.Title,
                    request.Screen.ViewState,
                    request.Screen.Annotations,
                    request.App.ContractCount),
                Events = request.Events,
                ResponseObject = new
                {
                    preferredLayout = response.PreferredLayout,
                    suggestions = response.Suggestions,
                },
            },
            cancellationToken);
        return response;
    }

    public CollaborationAdviseResponse Advise(
        CollaborationAdviseRequest request,
        CollaborationTendencyBundleDto profile)
    {
        var promptPreview = BuildPromptPreview(request, profile);
        var suggestions = BuildStubSuggestions(request, profile);
        var preferredLayout = BuildPreferredLayout(request, profile);

        return new CollaborationAdviseResponse(promptPreview, suggestions, preferredLayout);
    }

    public static string BuildPromptPreview(
        CollaborationAdviseRequest request,
        CollaborationTendencyBundleDto profile)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "You adapt the Select Contract UI to this user's preferred view styles.");
        sb.AppendLine(
            "Infer preferences for signals display (values vs graph) and detail level (summary vs extended),");
        sb.AppendLine(
            "then suggest UI adaptations the client can apply directly.");
        sb.AppendLine();
        sb.AppendLine(CollaborationContextFormatter.FormatExpectationGuidance());
        sb.AppendLine();
        sb.AppendLine(request.App.DomainDescription);
        sb.AppendLine(
            $"Portfolio size this turn: {request.App.ContractCount} contract(s).");
        sb.AppendLine();

        sb.AppendLine($"Screen: {request.Screen.Title} ({request.Screen.ScreenId})");
        if (request.Screen.Annotations is { Count: > 0 })
        {
            sb.AppendLine("Screen annotations:");
            foreach (var pair in request.Screen.Annotations)
            {
                sb.AppendLine($"  {pair.Key}: {pair.Value}");
            }
        }

        sb.AppendLine(CollaborationContextFormatter.FormatViewState(
            request.Screen.ViewState,
            request.App.ContractCount));
        sb.AppendLine(
            CollaborationContextFormatter.FormatComparisonPattern(
                request.Screen.ViewState,
                request.App.ContractCount,
                request.Events));
        sb.AppendLine(
            "Available adaptations: " + string.Join(", ", request.Screen.AvailableActions));
        sb.AppendLine();

        sb.AppendLine("Controls (semantic snapshots):");
        foreach (var control in request.Controls)
        {
            var detailLevel = control.Expanded ? "extended" : "summary";
            sb.AppendLine(
                $"- [{control.ControlType}] {control.Label} ({control.ControlId}) — detailLevel={detailLevel}");
            if (control.Annotations is { Count: > 0 })
            {
                foreach (var pair in control.Annotations)
                {
                    sb.AppendLine($"    annotation.{pair.Key}: {pair.Value}");
                }
            }

            sb.AppendLine("    commercial signals:");
            foreach (var pair in control.Data)
            {
                sb.AppendLine($"      {pair.Key}: {pair.Value}");
            }

            if (control.DetailData is { Count: > 0 })
            {
                sb.AppendLine("    extended detail:");
                foreach (var pair in control.DetailData)
                {
                    sb.AppendLine($"      {pair.Key}: {pair.Value}");
                }
            }
        }

        sb.AppendLine();
        sb.AppendLine(CollaborationContextFormatter.FormatRetrievedProfile(profile));
        sb.AppendLine();
        sb.AppendLine(CollaborationContextFormatter.FormatSemanticActions(request.Events));
        sb.AppendLine();
        sb.AppendLine(CollaborationContextFormatter.FormatActionTiming(request.Events));
        sb.AppendLine();
        sb.AppendLine("Return JSON with:");
        sb.AppendLine(
            "- preferredLayout: { expandAll: boolean, signalsDisplay: \"values\"|\"graph\"|null, "
            + "rationale: short string } — interpret activeSummary for durable cold-start layout");
        sb.AppendLine("- suggestions: applyable adaptations for this turn:");
        sb.AppendLine("  - kind=set-view with payload.signalsDisplay=values|graph");
        sb.AppendLine("  - kind=expand|collapse|select with targetControlId");

        return sb.ToString().TrimEnd();
    }

    public static IReadOnlyList<CollaborationSuggestionDto> BuildStubSuggestions(
        CollaborationAdviseRequest request,
        CollaborationTendencyBundleDto profile)
    {
        var selected = request.Events
            .Where(e => e.Type == "control.select")
            .OrderByDescending(e => e.At)
            .FirstOrDefault();

        if (selected?.ControlId is not null)
        {
            return
            [
                new CollaborationSuggestionDto(
                    "select-contract",
                    "select",
                    $"Select {selected.Label ?? selected.ControlId} to continue staffing",
                    selected.ControlId,
                    null),
            ];
        }

        var profileText = (profile.UserOverride ?? profile.AppDefaults ?? string.Empty)
            .ToLowerInvariant();
        var prefersGraph =
            profileText.Contains("graph", StringComparison.Ordinal)
            || request.Events.Any(e =>
                e.Type == "view.change"
                && string.Equals(
                    e.Meta?.GetValueOrDefault("to"),
                    "graph",
                    StringComparison.OrdinalIgnoreCase));
        var prefersValues =
            profileText.Contains("numeric", StringComparison.Ordinal)
            || profileText.Contains("values", StringComparison.Ordinal)
            || request.Events.Any(e =>
                e.Type == "view.change"
                && string.Equals(
                    e.Meta?.GetValueOrDefault("to"),
                    "values",
                    StringComparison.OrdinalIgnoreCase));

        var currentDisplay = request.Screen.ViewState.SignalsDisplay;
        if (prefersGraph
            && !string.Equals(currentDisplay, "graph", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                new CollaborationSuggestionDto(
                    "set-signals-graph",
                    "set-view",
                    "Switch signals to relative graph view (inferred preference)",
                    null,
                    new Dictionary<string, string> { ["signalsDisplay"] = "graph" }),
            ];
        }

        if (prefersValues
            && !prefersGraph
            && !string.Equals(currentDisplay, "values", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                new CollaborationSuggestionDto(
                    "set-signals-values",
                    "set-view",
                    "Switch signals to numeric values view (inferred preference)",
                    null,
                    new Dictionary<string, string> { ["signalsDisplay"] = "values" }),
            ];
        }

        var prefersExpandAll = PrefersExpandAllFromProfile(profileText);

        var collapsed = request.Controls.Where(c => !c.Expanded).ToList();
        if (prefersExpandAll && collapsed.Count > 0)
        {
            return collapsed
                .Select((control, index) => new CollaborationSuggestionDto(
                    $"expand-all-{index}",
                    "expand",
                    $"Show extended detail for {control.Label} (expand-all preference)",
                    control.ControlId,
                    new Dictionary<string, string> { ["detailLevel"] = "extended" }))
                .ToList();
        }

        var prefersExtended =
            profileText.Contains("extended", StringComparison.Ordinal)
            || request.Events.Count(e => e.Type == "control.expand")
                >= request.Events.Count(e => e.Type == "control.collapse");

        var firstCollapsed = collapsed.FirstOrDefault();
        if (prefersExtended && firstCollapsed is not null)
        {
            return
            [
                new CollaborationSuggestionDto(
                    "expand-next",
                    "expand",
                    $"Show extended detail for {firstCollapsed.Label}",
                    firstCollapsed.ControlId,
                    new Dictionary<string, string> { ["detailLevel"] = "extended" }),
            ];
        }

        var first = request.Controls.FirstOrDefault();
        if (first is null)
        {
            return [];
        }

        if (!first.Expanded)
        {
            return
            [
                new CollaborationSuggestionDto(
                    "expand-first",
                    "expand",
                    $"Show extended detail for {first.Label}",
                    first.ControlId,
                    new Dictionary<string, string> { ["detailLevel"] = "extended" }),
            ];
        }

        return
        [
            new CollaborationSuggestionDto(
                "select-first",
                "select",
                $"Select {first.Label} to continue staffing",
                first.ControlId,
                null),
        ];
    }

    public static CollaborationPreferredLayoutDto BuildPreferredLayout(
        CollaborationAdviseRequest request,
        CollaborationTendencyBundleDto profile)
    {
        var profileText = (profile.UserOverride ?? profile.AppDefaults ?? string.Empty)
            .ToLowerInvariant();
        var summaryFirst = PrefersSummaryFirstFromProfile(profileText);
        // Expand-all cues win when both appear (profile being rewritten); summary-first alone collapses.
        var expandAll = PrefersExpandAllFromProfile(profileText);

        string? signalsDisplay = null;
        if (profileText.Contains("value-first", StringComparison.Ordinal)
            || profileText.Contains("values over graphs", StringComparison.Ordinal)
            || profileText.Contains("numeric signal values over graphs", StringComparison.Ordinal))
        {
            signalsDisplay = "values";
        }
        else if (profileText.Contains("graph", StringComparison.Ordinal)
            && (profileText.Contains("prefer", StringComparison.Ordinal)
                || profileText.Contains("likes", StringComparison.Ordinal))
            && !profileText.Contains("over graphs", StringComparison.Ordinal))
        {
            signalsDisplay = "graph";
        }

        var rationale = expandAll
            ? $"Stub fallback: expand-all-before-select inferred for {request.App.ContractCount} contract(s)."
            : summaryFirst
                ? "Stub fallback: summary-first / don't-force inferred; leave cards collapsed."
                : "Stub fallback: no clear expand-all habit; leave cards collapsed.";

        return new CollaborationPreferredLayoutDto(expandAll, signalsDisplay, rationale);
    }

    public static string HumanizeEvent(CollaborationInteractionEventDto evt)
    {
        var target = evt.Label ?? evt.ControlId ?? "unknown";
        var meta = evt.Meta;
        return evt.Type switch
        {
            "screen.enter" => "Entered screen",
            "screen.leave" => "Left screen",
            "view.change" => DescribeViewChange(meta),
            "control.expand" =>
                $"Expanded detail on {target} (summary → extended"
                + DescribeDisplaySuffix(meta)
                + ")",
            "control.collapse" =>
                $"Collapsed detail on {target} (extended → summary"
                + DescribeDisplaySuffix(meta)
                + ")",
            "control.select" => $"Selected {target} to proceed",
            "signal.focus" =>
                $"Inspected signal {meta?.GetValueOrDefault("signalId") ?? evt.Label ?? "signal"}"
                + $" on {evt.ControlId ?? "control"}"
                + DescribeDisplaySuffix(meta),
            "signal.activate" =>
                $"Activated signal {meta?.GetValueOrDefault("signalId") ?? evt.Label ?? "signal"}"
                + $" on {evt.ControlId ?? "control"}"
                + DescribeDisplaySuffix(meta),
            _ => $"{evt.Type}: {target}",
        };
    }

    private static string DescribeViewChange(IReadOnlyDictionary<string, string>? meta)
    {
        var axis = meta?.GetValueOrDefault("preferenceAxis") ?? "view";
        var from = meta?.GetValueOrDefault("from") ?? "?";
        var to = meta?.GetValueOrDefault("to") ?? "?";
        var meaning = meta?.GetValueOrDefault("meaning");
        var suffix = string.IsNullOrWhiteSpace(meaning) ? string.Empty : $" ({meaning})";
        return $"Changed {axis} from {from} to {to}{suffix}";
    }

    private static string DescribeDisplaySuffix(IReadOnlyDictionary<string, string>? meta)
    {
        var display = meta?.GetValueOrDefault("signalsDisplay");
        return string.IsNullOrWhiteSpace(display) ? string.Empty : $"; signalsDisplay={display}";
    }

    private static bool PrefersSummaryFirstFromProfile(string profileText) =>
        profileText.Contains("without opening every", StringComparison.Ordinal)
        || profileText.Contains("without expanding every", StringComparison.Ordinal)
        || profileText.Contains("without expanding all", StringComparison.Ordinal)
        || profileText.Contains("don't force", StringComparison.Ordinal)
        || profileText.Contains("do not force", StringComparison.Ordinal)
        || profileText.Contains("choose from summary", StringComparison.Ordinal)
        || profileText.Contains("can choose from summary", StringComparison.Ordinal)
        || profileText.Contains("select from summary", StringComparison.Ordinal)
        || profileText.Contains("summary cards without", StringComparison.Ordinal);

    private static bool PrefersExpandAllFromProfile(string profileText) =>
        profileText.Contains("expand all", StringComparison.Ordinal)
        || profileText.Contains("expand-all", StringComparison.Ordinal)
        || profileText.Contains("expanding all visible", StringComparison.Ordinal)
        || profileText.Contains("expands all visible", StringComparison.Ordinal)
        || profileText.Contains("across all visible", StringComparison.Ordinal)
        || profileText.Contains("opens all visible", StringComparison.Ordinal)
        || profileText.Contains("opening all visible", StringComparison.Ordinal)
        || profileText.Contains("compare-all", StringComparison.Ordinal)
        || profileText.Contains("compare the full set", StringComparison.Ordinal)
        || profileText.Contains("full set in extended", StringComparison.Ordinal)
        || profileText.Contains("analysis-heavy comparison", StringComparison.Ordinal)
        || profileText.Contains("analyze every contract", StringComparison.Ordinal)
        || profileText.Contains("analyze all contracts", StringComparison.Ordinal)
        || (profileText.Contains("all visible contracts", StringComparison.Ordinal)
            && (profileText.Contains("before deciding", StringComparison.Ordinal)
                || profileText.Contains("before choosing", StringComparison.Ordinal)
                || profileText.Contains("before selecting", StringComparison.Ordinal)));
}
