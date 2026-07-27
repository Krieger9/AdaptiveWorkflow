using System.Text;
using AdaptiveTeamBuilder.Data.Contracts;

namespace AdaptiveTeamBuilderSvc;

/// <summary>
/// Shared formatting for retrieved user profile + turn context used in agent prompts and transcripts.
/// </summary>
public static class CollaborationContextFormatter
{
    /// <summary>Expand→collapse same control within this window is treated as a likely accidental toggle.</summary>
    public const int LikelyMistakeMs = 1500;

    /// <summary>Open expand held at least this long before the next change is treated as deliberate dwell.</summary>
    public const int DeliberateDwellMs = 4000;

    private static readonly HashSet<string> ChangeActionTypes =
    [
        "control.expand",
        "control.collapse",
        "control.select",
        "view.change",
        "signal.activate",
    ];

    public static string ActiveProfileSummary(CollaborationTendencyBundleDto profile) =>
        string.IsNullOrWhiteSpace(profile.UserOverride)
            ? profile.AppDefaults
            : profile.UserOverride!;

    public static string FormatRetrievedProfile(CollaborationTendencyBundleDto profile)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Retrieved user collaboration profile (from store):");
        sb.AppendLine($"  source: {profile.Source}");
        sb.AppendLine(
            "  updatedAt: "
            + (profile.UpdatedAt is { } at ? at.ToString("O") : "(never)"));
        sb.AppendLine("  appDefaults:");
        sb.AppendLine(Indent(profile.AppDefaults.Trim()));
        sb.AppendLine("  userOverride:");
        sb.AppendLine(
            string.IsNullOrWhiteSpace(profile.UserOverride)
                ? "    (none — using appDefaults)"
                : Indent(profile.UserOverride.Trim()));
        sb.AppendLine("  activeSummary (what the agent should treat as current tendencies):");
        sb.AppendLine(Indent(ActiveProfileSummary(profile).Trim()));
        return sb.ToString().TrimEnd();
    }

    public static string FormatViewState(
        CollaborationViewStateDto viewState,
        int? visibleControlCount = null)
    {
        var expandedCount = viewState.ExpandedControlIds.Count;
        var sb = new StringBuilder();
        sb.AppendLine("Current UI view state:");
        sb.AppendLine($"  signalsDisplay: {viewState.SignalsDisplay}");
        sb.AppendLine($"  expandedCount: {expandedCount}");
        if (visibleControlCount is int visible)
        {
            sb.AppendLine($"  visibleControlCount: {visible}");
            sb.AppendLine(
                $"  expandedCoverage: {expandedCount}/{visible}"
                + DescribeCoverage(expandedCount, visible));
        }

        sb.AppendLine(
            "  expandedControlIds: "
            + (expandedCount == 0
                ? "(none — all cards summary)"
                : string.Join(", ", viewState.ExpandedControlIds)));
        return sb.ToString().TrimEnd();
    }

    public static string FormatComparisonPattern(
        CollaborationViewStateDto? viewState,
        int? visibleControlCount,
        IReadOnlyList<CollaborationInteractionEventDto> events)
    {
        var expandedCount = viewState?.ExpandedControlIds.Count ?? 0;
        var expandEvents = events.Count(e => e.Type == "control.expand");
        var collapseEvents = events.Count(e => e.Type == "control.collapse");
        var selected = events.Any(e => e.Type == "control.select");
        var visible = visibleControlCount ?? 0;

        var sb = new StringBuilder();
        sb.AppendLine("Derived comparison pattern cues (use these when updating tendencies):");
        sb.AppendLine($"  expandsThisTurn: {expandEvents}");
        sb.AppendLine($"  collapsesThisTurn: {collapseEvents}");
        sb.AppendLine($"  selectedThisTurn: {selected}");
        sb.AppendLine($"  currentlyExpanded: {expandedCount}");
        if (visible > 0)
        {
            sb.AppendLine($"  visibleControls: {visible}");
        }

        if (visible > 1 && expandedCount >= visible && selected)
        {
            sb.AppendLine(
                "  inferredPattern: expand-all-then-select — user opened extended detail on every "
                + "visible contract before choosing one. Prefer recording that they like to analyze "
                + "all contracts in extended detail before selecting.");
        }
        else if (visible > 1 && expandedCount >= visible)
        {
            sb.AppendLine(
                "  inferredPattern: expand-all-compare — user currently has every visible contract "
                + "in extended detail. Prefer recording a compare-all-extended tendency.");
        }
        else if (expandEvents >= 2 && collapseEvents == 0)
        {
            sb.AppendLine(
                "  inferredPattern: expand-many — user opened multiple cards without collapsing. "
                + "Lean toward comparing several contracts in extended detail.");
        }
        else if (expandedCount == 1 && selected)
        {
            sb.AppendLine(
                "  inferredPattern: expand-one-then-select — user may prefer inspecting a single "
                + "card deeply before choosing.");
        }
        else if (expandedCount == 0 && selected)
        {
            sb.AppendLine(
                "  inferredPattern: select-from-summary — user selected from summary cards without "
                + "extended detail.");
        }
        else
        {
            sb.AppendLine(
                "  inferredPattern: inconclusive — combine event sequence with expandedCoverage.");
        }

        return sb.ToString().TrimEnd();
    }

    public static string FormatActionTiming(IReadOnlyList<CollaborationInteractionEventDto> events)
    {
        var changes = events
            .Where(e => ChangeActionTypes.Contains(e.Type))
            .OrderBy(e => e.At)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine(
            "Action timing cues (gaps between change actions; ignore signal.focus for gaps):");
        if (changes.Count == 0)
        {
            sb.AppendLine("  (no change actions this turn)");
            return sb.ToString().TrimEnd();
        }

        for (var i = 0; i < changes.Count; i++)
        {
            var evt = changes[i];
            var gapMs = ResolveGapMs(evt, i == 0 ? null : changes[i - 1]);
            var gapText = gapMs is long ms ? $"{ms}ms since previous change" : "first change in batch";
            sb.AppendLine(
                $"  {i + 1}) {evt.Type} on {evt.ControlId ?? evt.Label ?? "?"} — {gapText}");
        }

        for (var i = 1; i < changes.Count; i++)
        {
            var prev = changes[i - 1];
            var curr = changes[i];
            var gapMs = ResolveGapMs(curr, prev);
            if (gapMs is not long gap)
            {
                continue;
            }

            if (prev.Type == "control.expand"
                && curr.Type == "control.collapse"
                && SameControl(prev, curr)
                && gap <= LikelyMistakeMs)
            {
                sb.AppendLine(
                    $"  flag: likely-mistake — expand→collapse on {curr.ControlId} within {gap}ms "
                    + "(<={LikelyMistakeMs}ms); down-weight as accidental toggle.");
            }
            else if (prev.Type == "control.expand" && gap >= DeliberateDwellMs)
            {
                sb.AppendLine(
                    $"  flag: deliberate-dwell — expand on {prev.ControlId} held ~{gap}ms before "
                    + $"{curr.Type}; treat as intentional review.");
            }
        }

        return sb.ToString().TrimEnd();
    }

    public static string FormatSemanticActions(
        IReadOnlyList<CollaborationInteractionEventDto> events,
        string heading = "Recent semantic actions (what the user did this turn):")
    {
        var sb = new StringBuilder();
        sb.AppendLine(heading);
        if (events.Count == 0)
        {
            sb.AppendLine("(none recorded this turn)");
            return sb.ToString().TrimEnd();
        }

        var index = 1;
        foreach (var evt in events.OrderBy(e => e.At))
        {
            sb.AppendLine($"{index}) [{evt.At:O}] {StubCollaborationAdvisor.HumanizeEvent(evt)}");
            if (evt.Meta is { Count: > 0 })
            {
                foreach (var pair in evt.Meta)
                {
                    sb.AppendLine($"      meta.{pair.Key}: {pair.Value}");
                }
            }

            index++;
        }

        return sb.ToString().TrimEnd();
    }

    public static string FormatExpectationGuidance()
    {
        return
            """
            Expectation for this agent turn:
            - Read the retrieved activeSummary as the user's current tendencies.
            - Interpret semantic actions AND expandedCoverage as evidence of preferred view styles
              (signalsDisplay: values vs graph; detailLevel: summary vs extended;
              compareStyle: expand-one vs expand-all-before-select).
            - Always return preferredLayout by interpreting activeSummary (especially on cold start
              with few/no change events): expandAll true when the durable habit is comparing all
              visible contracts in extended detail before choosing; expandAll false for summary-first
              / choose-without-opening-every / don't-force. Boilerplate "start with summary" must not
              override a clear expand-all-before-select habit. Set signalsDisplay to values|graph when
              the profile has a clear preference; otherwise null.
            - preferredLayout is what the client auto-applies on load; suggestions remain interactive
              adaptations (set-view / expand / collapse / select) for the current turn.
            - If expandedCoverage is all/most visible contracts and the user then selects,
              record that they like expanding and analyzing all contracts before choosing.
            - Use action timing (sincePreviousMs / timing cues) to down-weight accidental toggles
              (likely-mistake: quick expand→collapse) and trust deliberate-dwell / slow gaps more.
            - Prefer durable preference updates / UI adaptations the client can apply directly.
            - Keep TendencyProse concise; rewrite noisy stub append logs into clean preferences.
            - Do not invent control IDs; use only ids present in the control snapshots.
            """.Trim();
    }

    private static long? ResolveGapMs(
        CollaborationInteractionEventDto current,
        CollaborationInteractionEventDto? previous)
    {
        if (current.Meta is not null
            && current.Meta.TryGetValue("sincePreviousMs", out var raw)
            && long.TryParse(raw, out var fromMeta))
        {
            return fromMeta;
        }

        if (previous is null)
        {
            return null;
        }

        return Math.Max(0, (long)(current.At - previous.At).TotalMilliseconds);
    }

    private static bool SameControl(
        CollaborationInteractionEventDto a,
        CollaborationInteractionEventDto b) =>
        !string.IsNullOrWhiteSpace(a.ControlId)
        && string.Equals(a.ControlId, b.ControlId, StringComparison.Ordinal);

    private static string DescribeCoverage(int expandedCount, int visible)
    {
        if (visible <= 0)
        {
            return string.Empty;
        }

        if (expandedCount >= visible)
        {
            return " (ALL visible contracts expanded)";
        }

        if (expandedCount == 0)
        {
            return " (none expanded)";
        }

        if (expandedCount >= Math.Ceiling(visible * 0.67))
        {
            return " (MOST visible contracts expanded)";
        }

        return " (partial)";
    }

    private static string Indent(string text, string prefix = "    ")
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return prefix + "(empty)";
        }

        return string.Join(
            Environment.NewLine,
            text.Replace("\r\n", "\n").Split('\n').Select(line => prefix + line));
    }
}
