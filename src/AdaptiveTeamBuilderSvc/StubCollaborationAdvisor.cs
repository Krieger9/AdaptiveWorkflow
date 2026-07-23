using System.Text;
using AdaptiveTeamBuilder.Data.Contracts;

namespace AdaptiveTeamBuilderSvc;

public interface ICollaborationAdvisor
{
    CollaborationAdviseResponse Advise(CollaborationAdviseRequest request);
}

/// <summary>
/// Placeholder for a future LLM-backed advisor. Builds a prompt preview and
/// synthesizes updated tendency prose from the turn's interaction events.
/// </summary>
public sealed class StubCollaborationAdvisor : ICollaborationAdvisor
{
    public CollaborationAdviseResponse Advise(CollaborationAdviseRequest request)
    {
        var promptPreview = BuildPromptPreview(request);
        var updatedTendencies = BuildUpdatedTendencies(request);
        var suggestions = BuildStubSuggestions(request);

        return new CollaborationAdviseResponse(promptPreview, updatedTendencies, suggestions);
    }

    private static string BuildPromptPreview(CollaborationAdviseRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine(request.App.DomainDescription);
        sb.AppendLine(
            $"We are attempting to staff {request.App.ContractCount} contract(s).");
        sb.AppendLine();
        sb.AppendLine("These are the data sets we are working with:");
        var index = 1;
        foreach (var summary in request.App.DatasetSummaries)
        {
            sb.AppendLine($"{index}) {summary}");
            index++;
        }

        sb.AppendLine();
        sb.AppendLine($"Screen: {request.Screen.Title} ({request.Screen.ScreenId})");
        sb.AppendLine("Available actions: " + string.Join(", ", request.Screen.AvailableActions));
        sb.AppendLine();
        sb.AppendLine("Shown controls:");
        foreach (var control in request.Controls)
        {
            var state = control.Expanded ? "expanded" : "collapsed";
            sb.AppendLine($"- [{control.ControlType}] {control.Label} ({control.ControlId}) — {state}");
            foreach (var pair in control.Data)
            {
                sb.AppendLine($"    {pair.Key}: {pair.Value}");
            }

            if (control.DetailData is { Count: > 0 })
            {
                sb.AppendLine("    detail:");
                foreach (var pair in control.DetailData)
                {
                    sb.AppendLine($"      {pair.Key}: {pair.Value}");
                }
            }
        }

        sb.AppendLine();
        var activeTendencies = string.IsNullOrWhiteSpace(request.Tendencies.UserOverride)
            ? request.Tendencies.AppDefaults
            : request.Tendencies.UserOverride;
        sb.AppendLine("Current user tendencies:");
        sb.AppendLine(activeTendencies);
        sb.AppendLine();
        sb.AppendLine("The user took these actions:");
        if (request.Events.Count == 0)
        {
            sb.AppendLine("(none recorded this turn)");
        }
        else
        {
            var actionIndex = 1;
            foreach (var evt in request.Events.OrderBy(e => e.At))
            {
                sb.AppendLine($"{actionIndex}) {HumanizeEvent(evt)}");
                actionIndex++;
            }
        }

        sb.AppendLine();
        sb.AppendLine("Update our user tendencies.");
        sb.AppendLine("[Tools provided for updating user tendencies]");

        return sb.ToString().TrimEnd();
    }

    private static CollaborationTendencyBundleDto BuildUpdatedTendencies(
        CollaborationAdviseRequest request)
    {
        var observations = SummarizeEvents(request.Events);
        var baseProse = string.IsNullOrWhiteSpace(request.Tendencies.UserOverride)
            ? request.Tendencies.AppDefaults
            : request.Tendencies.UserOverride!;

        string updated;
        if (string.IsNullOrWhiteSpace(observations))
        {
            updated =
                baseProse.Trim()
                + "\n\n(Stub advisor) No new interaction signals this turn; tendencies unchanged in substance.";
        }
        else
        {
            updated =
                baseProse.Trim()
                + "\n\n(Stub advisor) From the latest Select Contract turn: "
                + observations;
        }

        return new CollaborationTendencyBundleDto(
            request.Tendencies.AppDefaults,
            updated,
            DateTime.UtcNow,
            "stub");
    }

    private static IReadOnlyList<CollaborationSuggestionDto> BuildStubSuggestions(
        CollaborationAdviseRequest request)
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
                    "navigate-selected",
                    "next",
                    $"Navigate to staffing for {selected.Label ?? selected.ControlId}",
                    selected.ControlId),
            ];
        }

        var first = request.Controls.FirstOrDefault();
        if (first is null)
        {
            return [];
        }

        return
        [
            new CollaborationSuggestionDto(
                "expand-first",
                "next",
                $"Expand details for {first.Label} (left-to-right default)",
                first.ControlId),
        ];
    }

    private static string SummarizeEvents(
        IReadOnlyList<CollaborationInteractionEventDto> events)
    {
        if (events.Count == 0)
        {
            return string.Empty;
        }

        var expanded = events.Count(e => e.Type == "control.expand");
        var collapsed = events.Count(e => e.Type == "control.collapse");
        var focuses = events
            .Where(e => e.Type == "signal.focus")
            .Select(e => e.Label ?? e.Meta?.GetValueOrDefault("signal") ?? "a signal")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var selected = events
            .Where(e => e.Type == "control.select")
            .Select(e => e.Label ?? e.ControlId)
            .LastOrDefault();

        var parts = new List<string>();
        if (expanded > 0)
        {
            parts.Add($"expanded {expanded} contract card(s)");
        }

        if (collapsed > 0)
        {
            parts.Add($"collapsed {collapsed} card(s)");
        }

        if (focuses.Count > 0)
        {
            parts.Add("inspected signals: " + string.Join(", ", focuses));
        }

        if (!string.IsNullOrWhiteSpace(selected))
        {
            parts.Add($"selected {selected}");
        }

        if (parts.Count == 0)
        {
            return "recorded " + events.Count + " UI interaction(s).";
        }

        return string.Join("; ", parts) + ".";
    }

    private static string HumanizeEvent(CollaborationInteractionEventDto evt)
    {
        var target = evt.Label ?? evt.ControlId ?? "unknown";
        return evt.Type switch
        {
            "screen.enter" => "Entered screen",
            "screen.leave" => "Left screen",
            "control.expand" => $"Expanded {target}",
            "control.collapse" => $"Collapsed {target}",
            "control.select" => $"Selected {target}",
            "signal.focus" => $"Highlighted {evt.Label ?? "signal"} on {evt.ControlId ?? "control"}",
            _ => $"{evt.Type}: {target}",
        };
    }
}
