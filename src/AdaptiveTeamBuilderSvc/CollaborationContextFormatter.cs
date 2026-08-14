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

        // Auto-applied expand-all often yields expandsThisTurn=0 with only collapses buffered.
        // Returning to keep-top-two often expands 2 of 3 with zero collapses (third never opened).
        if (visible >= 3
            && expandedCount == visible - 1
            && selected)
        {
            sb.AppendLine(
                "  inferredPattern: keep-top-two-then-select — user kept exactly one card collapsed "
                + "(or never opened it) and selected from a two-card extended set. Prefer recording "
                + "a compare-top-subset habit; combine with commercial signal rank cues for which "
                + "metric ranked the expanded pair #1+#2.");
        }
        else if (visible >= 3
            && expandedCount == visible - 1
            && collapseEvents >= 1)
        {
            sb.AppendLine(
                "  inferredPattern: keep-top-two-compare — user has two of three cards extended. "
                + "Lean toward comparing a subset, not expand-all or expand-one.");
        }
        else if (visible > 1 && expandedCount >= visible && selected)
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
        else if (expandEvents >= 2 && collapseEvents == 0 && expandedCount >= 2)
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

    /// <summary>
    /// Ranks commercial signals across visible contract cards and notes where expanded/selected/
    /// collapsed cards sit — evidence for preferred commercial signal (e.g. Margin vs Profit).
    /// </summary>
    public static string FormatSignalRankComparison(
        IReadOnlyList<CollaborationControlSnapshotDto>? controls,
        CollaborationViewStateDto? viewState,
        IReadOnlyList<CollaborationInteractionEventDto>? events = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "Commercial signal rank cues (visible portfolio this turn; #1 = highest numeric):");

        if (controls is null || controls.Count == 0)
        {
            sb.AppendLine("  (no control snapshots)");
            return sb.ToString().TrimEnd();
        }

        var eventList = events ?? [];
        var expandedIds = new HashSet<string>(
            viewState?.ExpandedControlIds ?? [],
            StringComparer.OrdinalIgnoreCase);
        foreach (var control in controls.Where(c => c.Expanded))
        {
            expandedIds.Add(control.ControlId);
        }

        var selectedIds = new HashSet<string>(
            eventList
                .Where(e => e.Type == "control.select" && !string.IsNullOrWhiteSpace(e.ControlId))
                .Select(e => e.ControlId!),
            StringComparer.OrdinalIgnoreCase);

        var collapsedIds = new HashSet<string>(
            eventList
                .Where(e => e.Type == "control.collapse" && !string.IsNullOrWhiteSpace(e.ControlId))
                .Select(e => e.ControlId!),
            StringComparer.OrdinalIgnoreCase);

        var rankedSignals = new (string Key, string Label)[]
        {
            ("estimatedContractValue", "Value"),
            ("estimatedProfit", "Profit"),
            ("estimatedMarginPercent", "Margin"),
            ("winProbabilityPercent", "Win prob."),
        };

        var keepTopTwoSignals = new List<string>();
        var collapseLowestSignals = new List<string>();
        var selectHighSignals = new List<string>();

        foreach (var (key, label) in rankedSignals)
        {
            var ranked = RankControlsBySignal(controls, key);
            if (ranked.Count == 0)
            {
                continue;
            }

            sb.AppendLine(
                $"  {label} order: "
                + string.Join(
                    " > ",
                    ranked.Select(r => $"{ShortLabel(r.Control)}=#{r.Rank}({FormatSignalValue(r.Raw)})")));

            var expandedRanks = ranked
                .Where(r => expandedIds.Contains(r.Control.ControlId))
                .Select(r => r.Rank)
                .OrderBy(r => r)
                .ToList();
            if (expandedRanks.Count > 0)
            {
                sb.AppendLine(
                    $"    expanded on {label}: {string.Join(", ", expandedRanks.Select(r => $"#{r}"))}");
            }

            var collapsedRanks = ranked
                .Where(r => collapsedIds.Contains(r.Control.ControlId))
                .Select(r => r.Rank)
                .OrderBy(r => r)
                .ToList();
            if (collapsedRanks.Count > 0)
            {
                sb.AppendLine(
                    $"    collapsed on {label}: {string.Join(", ", collapsedRanks.Select(r => $"#{r}"))}");
            }

            var selectedRanks = ranked
                .Where(r => selectedIds.Contains(r.Control.ControlId))
                .Select(r => r.Rank)
                .OrderBy(r => r)
                .ToList();
            if (selectedRanks.Count > 0)
            {
                sb.AppendLine(
                    $"    selected on {label}: {string.Join(", ", selectedRanks.Select(r => $"#{r}"))}");
            }

            var lowestRank = ranked.Count;
            var keptTopTwo = expandedRanks.Count >= 2
                && expandedRanks.Take(2).SequenceEqual(new[] { 1, 2 });
            var collapsedLowest = collapsedRanks.Count > 0
                && collapsedRanks.All(r => r == lowestRank)
                && collapsedRanks.Contains(lowestRank);
            var selectedHigh = selectedRanks.Count > 0 && selectedRanks.Min() <= 2;

            if (keptTopTwo)
            {
                keepTopTwoSignals.Add(label);
            }

            if (collapsedLowest)
            {
                collapseLowestSignals.Add(label);
            }

            if (selectedHigh)
            {
                selectHighSignals.Add(label);
            }

            if (keptTopTwo && collapsedLowest)
            {
                sb.AppendLine(
                    $"    pattern: keep-top-2 / collapse-lowest on {label} — strong evidence this "
                    + "signal drives which cards stay open.");
            }
            else if (keptTopTwo)
            {
                sb.AppendLine(
                    $"    pattern: keep-top-2 on {label} — expanded set matches the two highest.");
            }
            else if (collapsedLowest)
            {
                sb.AppendLine(
                    $"    pattern: collapse-lowest on {label} — discarded the weakest card on this "
                    + "metric.");
            }
        }

        if (keepTopTwoSignals.Count > 0 || collapseLowestSignals.Count > 0 || selectHighSignals.Count > 0)
        {
            sb.AppendLine(
                "  turn summary: "
                + (keepTopTwoSignals.Count > 0
                    ? $"keep-top-2 signals=[{string.Join(", ", keepTopTwoSignals)}]; "
                    : string.Empty)
                + (collapseLowestSignals.Count > 0
                    ? $"collapse-lowest signals=[{string.Join(", ", collapseLowestSignals)}]; "
                    : string.Empty)
                + (selectHighSignals.Count > 0
                    ? $"select-high signals=[{string.Join(", ", selectHighSignals)}]."
                    : string.Empty).TrimEnd());
        }

        // Cross-signal elimination: collapsed card high on A but lowest on B.
        foreach (var collapsedId in collapsedIds)
        {
            var ranksBySignal = new List<string>();
            foreach (var (key, label) in rankedSignals)
            {
                var ranked = RankControlsBySignal(controls, key);
                var hit = ranked.FirstOrDefault(r =>
                    string.Equals(r.Control.ControlId, collapsedId, StringComparison.OrdinalIgnoreCase));
                if (hit.Control is not null)
                {
                    ranksBySignal.Add($"{label}=#{hit.Rank}");
                }
            }

            if (ranksBySignal.Count > 0)
            {
                var control = controls.FirstOrDefault(c =>
                    string.Equals(c.ControlId, collapsedId, StringComparison.OrdinalIgnoreCase));
                var collapsedLabel = control is null ? collapsedId : ShortLabel(control);
                sb.AppendLine(
                    $"  collapsed card {collapsedLabel} ranks: "
                    + string.Join(", ", ranksBySignal)
                    + " — if this card is high on Profit/Value but lowest on Margin (or vice versa), "
                    + "prefer the metric where it was discarded.");
            }
        }

        sb.AppendLine(
            "  inference hint: prefer signals with keep-top-2, collapse-lowest, OR expand-one+"
            + "select-high this turn. Cross-ranks on the collapsed card eliminate metrics where "
            + "that card was strong. Habit shifts are bidirectional: if activeSummary says "
            + "expand-one by Profit but this turn/recent digests show keep-top-2 (expanded=2/3, "
            + "Profit #1+#2), UNDERMINE expand-one and move to keep-top-two — and the reverse. "
            + "Likewise Margin ↔ Profit. Commit when ≥2 of the last ~5 digests agree; one "
            + "contradiction may mark the prior habit as under review. Do not preserve a "
            + "contradicted durable claim.");

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

    /// <summary>
    /// Compact one-line digest of a decision turn for rolling recent-observation memory.
    /// </summary>
    public static string? FormatDecisionTurnDigest(
        IReadOnlyList<CollaborationControlSnapshotDto>? controls,
        CollaborationViewStateDto? viewState,
        IReadOnlyList<CollaborationInteractionEventDto> events,
        string? activeSummary = null,
        string? screenId = null)
    {
        var isDecision = events.Any(e =>
            e.Type is "control.select" or "view.change");
        if (!isDecision)
        {
            return null;
        }

        var expandedCount = viewState?.ExpandedControlIds.Count
            ?? controls?.Count(c => c.Expanded)
            ?? 0;
        var visible = controls?.Count ?? 0;
        var pattern = InferPatternLabel(viewState, visible, events);

        var rankBits = new List<string>();
        if (controls is { Count: > 0 })
        {
            foreach (var (key, label) in new (string Key, string Label)[]
                     {
                         ("estimatedProfit", "Profit"),
                         ("estimatedMarginPercent", "Margin"),
                         ("estimatedContractValue", "Value"),
                         ("winProbabilityPercent", "Win"),
                     })
            {
                var ranked = RankControlsBySignal(controls, key);
                if (ranked.Count == 0)
                {
                    continue;
                }

                var selectedIds = events
                    .Where(e => e.Type == "control.select" && !string.IsNullOrWhiteSpace(e.ControlId))
                    .Select(e => e.ControlId!)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var expandedIds = new HashSet<string>(
                    viewState?.ExpandedControlIds ?? [],
                    StringComparer.OrdinalIgnoreCase);
                foreach (var c in controls.Where(c => c.Expanded))
                {
                    expandedIds.Add(c.ControlId);
                }

                var sel = ranked.Where(r => selectedIds.Contains(r.Control.ControlId)).Select(r => r.Rank).ToList();
                var exp = ranked.Where(r => expandedIds.Contains(r.Control.ControlId)).Select(r => r.Rank).ToList();
                if (sel.Count > 0 || exp.Count > 0)
                {
                    rankBits.Add(
                        $"{label}:exp={(exp.Count == 0 ? "-" : string.Join("+", exp.Select(r => $"#{r}")))}"
                        + $"/sel={(sel.Count == 0 ? "-" : string.Join("+", sel.Select(r => $"#{r}")))}");
                }
            }
        }

        var signalsDisplay = NormalizeSignalsDisplay(viewState?.SignalsDisplay);
        var contradiction = DetectContradictionFlags(activeSummary, pattern, rankBits, signalsDisplay);

        return
            $"{DateTime.UtcNow:yyyy-MM-ddTHH:mmZ} {(screenId ?? "select-contract")}: "
            + $"pattern={pattern}; expanded={expandedCount}/{visible}"
            + (signalsDisplay is null ? string.Empty : $"; display={signalsDisplay}")
            + (rankBits.Count > 0 ? "; " + string.Join("; ", rankBits) : string.Empty)
            + (contradiction is null ? string.Empty : "; " + contradiction);
    }

    /// <summary>
    /// Normalizes the raw signalsDisplay view-state value to a compact digest token
    /// ("graph" or "values"), or null when unknown so the axis is simply omitted.
    /// </summary>
    private static string? NormalizeSignalsDisplay(string? signalsDisplay)
    {
        if (string.IsNullOrWhiteSpace(signalsDisplay))
        {
            return null;
        }

        if (signalsDisplay.Contains("graph", StringComparison.OrdinalIgnoreCase))
        {
            return "graph";
        }

        if (signalsDisplay.Contains("value", StringComparison.OrdinalIgnoreCase)
            || signalsDisplay.Contains("numeric", StringComparison.OrdinalIgnoreCase))
        {
            return "values";
        }

        return null;
    }

    public static string FormatRecentTurnDigests(IReadOnlyList<string>? digests)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "Recent decision-turn digests (oldest→newest, last ~5; use to detect habit shifts "
            + "vs one-off actions):");
        if (digests is null || digests.Count == 0)
        {
            sb.AppendLine("  (none yet — only this turn's evidence is available)");
            return sb.ToString().TrimEnd();
        }

        for (var i = 0; i < digests.Count; i++)
        {
            sb.AppendLine($"  {i + 1}) {digests[i]}");
        }

        sb.AppendLine(
            "  digest rule: if ≥2 recent digests agree on a new pattern/signal and contradict "
            + "activeSummary, rewrite TendencyProse to the new habit and drop the old claim. "
            + "pattern=keep-top-two-then-select (or expanded=2/3 with keep-top-2 ranks) overrides "
            + "a sticky expand-one claim; expand-one digests override sticky keep-top-two. "
            + "The same rule applies to the display= token: if ≥2 recent digests show "
            + "display=graph while activeSummary claims numeric/values (or display=values vs a "
            + "graph claim), commit the switch, rewrite the durable signalsDisplay preference, and "
            + "set preferredLayout.signalsDisplay accordingly. "
            + "A single contradictory digest may mark the prior habit as under review.");
        return sb.ToString().TrimEnd();
    }

    private static string InferPatternLabel(
        CollaborationViewStateDto? viewState,
        int visible,
        IReadOnlyList<CollaborationInteractionEventDto> events)
    {
        var expandedCount = viewState?.ExpandedControlIds.Count ?? 0;
        var expandEvents = events.Count(e => e.Type == "control.expand");
        var collapseEvents = events.Count(e => e.Type == "control.collapse");
        var selected = events.Any(e => e.Type == "control.select");

        if (visible >= 3 && expandedCount == visible - 1 && selected)
        {
            return "keep-top-two-then-select";
        }

        if (visible >= 3 && expandedCount == visible - 1)
        {
            return "keep-top-two-compare";
        }

        if (visible > 1 && expandedCount >= visible && selected)
        {
            return "expand-all-then-select";
        }

        if (expandedCount == 1 && selected)
        {
            return "expand-one-then-select";
        }

        if (expandedCount == 0 && selected)
        {
            return "select-from-summary";
        }

        if (expandEvents >= 2 && collapseEvents == 0 && expandedCount >= 2)
        {
            return "expand-many";
        }

        return "other";
    }

    private static string? DetectContradictionFlags(
        string? activeSummary,
        string pattern,
        IReadOnlyList<string> rankBits,
        string? signalsDisplay = null)
    {
        if (string.IsNullOrWhiteSpace(activeSummary))
        {
            return null;
        }

        var summary = activeSummary.ToLowerInvariant();
        var flags = new List<string>();

        // signalsDisplay axis (graph vs numeric/values). Mirror the prose emitted by
        // SummarizePreferenceSignals ("prefers numeric values signal display over graphs" /
        // "prefers relative graph signal display over numeric values"). Key on the "X signal
        // display" ordering so the trailing "over Y" clause does not cross-trigger the opposite
        // claim.
        var claimsNumericDisplay = summary.Contains("values signal display", StringComparison.Ordinal)
            || summary.Contains("numeric signal display", StringComparison.Ordinal)
            || summary.Contains("signalsdisplay=values", StringComparison.Ordinal)
            || summary.Contains("signalsdisplay: values", StringComparison.Ordinal);
        var claimsGraphDisplay = summary.Contains("graph signal display", StringComparison.Ordinal)
            || summary.Contains("relative graph", StringComparison.Ordinal)
            || summary.Contains("signalsdisplay=graph", StringComparison.Ordinal)
            || summary.Contains("signalsdisplay: graph", StringComparison.Ordinal);

        if (claimsNumericDisplay
            && string.Equals(signalsDisplay, "graph", StringComparison.OrdinalIgnoreCase))
        {
            flags.Add("CONTRADICTS prior numeric-display claim (this turn graph)");
        }
        else if (claimsGraphDisplay
            && string.Equals(signalsDisplay, "values", StringComparison.OrdinalIgnoreCase))
        {
            flags.Add("CONTRADICTS prior graph-display claim (this turn numeric values)");
        }

        var claimsMargin = summary.Contains("margin", StringComparison.Ordinal)
            && (summary.Contains("prefer", StringComparison.Ordinal)
                || summary.Contains("durable", StringComparison.Ordinal)
                || summary.Contains("commercial-signal", StringComparison.Ordinal)
                || summary.Contains("commercial signal", StringComparison.Ordinal));
        var claimsProfit = summary.Contains("profit", StringComparison.Ordinal)
            && (summary.Contains("prefer", StringComparison.Ordinal)
                || summary.Contains("durable", StringComparison.Ordinal)
                || summary.Contains("commercial-signal", StringComparison.Ordinal)
                || summary.Contains("commercial signal", StringComparison.Ordinal));
        var claimsKeepTopTwo = summary.Contains("keep-top-two", StringComparison.Ordinal)
            || summary.Contains("top two", StringComparison.Ordinal)
            || summary.Contains("strongest two", StringComparison.Ordinal)
            || summary.Contains("two highest", StringComparison.Ordinal)
            || summary.Contains("expandTopCount=2", StringComparison.Ordinal)
            || summary.Contains("opens the two", StringComparison.Ordinal);
        var claimsExpandOne = summary.Contains("expand-one", StringComparison.Ordinal)
            || summary.Contains("expand one", StringComparison.Ordinal)
            || summary.Contains("opens a single", StringComparison.Ordinal)
            || summary.Contains("open a single", StringComparison.Ordinal)
            || summary.Contains("single card", StringComparison.Ordinal)
            || summary.Contains("one card", StringComparison.Ordinal)
            || summary.Contains("expandTopCount=1", StringComparison.Ordinal);

        var profitSelHigh = rankBits.Any(b =>
            b.StartsWith("Profit:", StringComparison.Ordinal)
            && b.Contains("sel=#1", StringComparison.Ordinal));
        var marginSelLow = rankBits.Any(b =>
            b.StartsWith("Margin:", StringComparison.Ordinal)
            && (b.Contains("sel=#3", StringComparison.Ordinal) || b.Contains("exp=#3", StringComparison.Ordinal)));
        var profitKeepTopTwo = rankBits.Any(b =>
            b.StartsWith("Profit:", StringComparison.Ordinal)
            && b.Contains("exp=#1+#2", StringComparison.Ordinal));

        if (claimsMargin && profitSelHigh && marginSelLow)
        {
            flags.Add("CONTRADICTS prior Margin claim (this turn Profit-high / Margin-low)");
        }

        if (claimsKeepTopTwo && pattern is "expand-one-then-select" or "select-from-summary")
        {
            flags.Add($"CONTRADICTS prior keep-top-two (this turn {pattern})");
        }

        if (claimsExpandOne && pattern is "keep-top-two-then-select" or "keep-top-two-compare")
        {
            flags.Add($"CONTRADICTS prior expand-one (this turn {pattern})");
        }

        if (claimsExpandOne && profitKeepTopTwo)
        {
            flags.Add("CONTRADICTS prior expand-one-by-Profit (this turn keep-top-2 on Profit)");
        }

        if (claimsProfit && marginSelLow == false && rankBits.Any(b =>
                b.StartsWith("Margin:", StringComparison.Ordinal) && b.Contains("sel=#1", StringComparison.Ordinal))
            && rankBits.Any(b =>
                b.StartsWith("Profit:", StringComparison.Ordinal) && b.Contains("sel=#3", StringComparison.Ordinal)))
        {
            flags.Add("CONTRADICTS prior Profit claim (this turn Margin-high / Profit-low)");
        }

        return flags.Count == 0 ? null : string.Join("; ", flags);
    }

    public static string FormatExpectationGuidance()
    {
        return
            """
            Expectation for this agent turn:
            - Read the retrieved activeSummary as the user's current tendencies.
            - Also read Recent decision-turn digests (last ~5). Treat them as stronger evidence for
              habit shifts than a single sticky sentence in activeSummary.
            - Interpret semantic actions AND expandedCoverage as evidence of preferred view styles
              (signalsDisplay: values vs graph; detailLevel: summary vs extended;
              compareStyle: expand-one vs expand-all-before-select vs keep-top-two-then-select).
            - Also interpret commercial signal rank cues, especially keep-top-2 / collapse-lowest /
              expand-one+select-high patterns and collapsed-card cross-ranks. Auto-applied expand-all
              may yield expandsThisTurn=0; treat final expanded set plus collapse events as primary.
            - Habit shift / rollback (critical):
              * Shifts are bidirectional. expand-one ↔ keep-top-two and Margin ↔ Profit can each
                reverse when recent digests disagree with activeSummary.
              * If this turn CONTRADICTS activeSummary (e.g. profile says expand-one by Profit but
                turn/digests show keep-top-two with Profit #1+#2 expanded), do NOT re-assert expand-one.
              * Mark the old habit as under review after one contradiction; after ≥2 agreeing
                recent digests on the new pattern/signal, COMMIT the new habit and remove the old
                durable commercial-signal / compareStyle claim from TendencyProse.
              * expanded=2/3 with keep-top-2 rank cues IS keep-top-two even with zero collapse events
                (the third card may never have been opened).
              * signalsDisplay shifts the same way: recent digests carry a display=graph|values
                token. If ≥2 recent digests contradict the stored signalsDisplay (e.g. profile says
                numeric/values but digests show display=graph, or the reverse), COMMIT the switch,
                rewrite the durable signalsDisplay preference, and set preferredLayout.signalsDisplay.
                Do not re-assert the old display preference out of loyalty to prior prose.
              * Preserve only preferences that this turn and recent digests still support.
            - Preferred commercial signal commitment:
              * Confirming turns on the SAME signal may commit (keep-top-2, collapse-lowest, or
                expand-one+select-high on that signal).
              * Confirming a NEW signal that contradicts the profile requires digest agreement
                (≥2 recent turns), not loyalty to the old prose.
            - Always return preferredLayout by interpreting the UPDATED tendencies (especially on
              cold start): expandAll true only for true expand-all; keep-top-two by signal →
              expandAll=false, expandTopCount=2, expandBySignal=...; expand-one by signal →
              expandAll=false, expandTopCount=1, expandBySignal=...; summary-first → expandAll=false
              with null expandTopCount. Set signalsDisplay when clear.
            - preferredLayout is what the client auto-applies on load; suggestions remain interactive
              adaptations (set-view / expand / collapse / select) for the current turn.
            - Use action timing to down-weight accidental toggles (likely-mistake) and trust
              deliberate-dwell / slow gaps more.
            - Keep TendencyProse concise; rewrite noisy stub append logs into clean preferences.
            - Do not invent control IDs; use only ids present in the control snapshots.
            """.Trim();
    }

    private static List<(CollaborationControlSnapshotDto Control, int Rank, string Raw)> RankControlsBySignal(
        IReadOnlyList<CollaborationControlSnapshotDto> controls,
        string signalKey)
    {
        var scored = new List<(CollaborationControlSnapshotDto Control, decimal Value, string Raw)>();
        foreach (var control in controls)
        {
            if (control.Data is null
                || !control.Data.TryGetValue(signalKey, out var raw)
                || !TryParseDecimal(raw, out var value))
            {
                continue;
            }

            scored.Add((control, value, raw));
        }

        return scored
            .OrderByDescending(s => s.Value)
            .Select((s, index) => (s.Control, Rank: index + 1, s.Raw))
            .ToList();
    }

    private static bool TryParseDecimal(string raw, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        return decimal.TryParse(
            raw.Trim().TrimEnd('%'),
            System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture,
            out value);
    }

    private static string ShortLabel(CollaborationControlSnapshotDto control)
    {
        if (control.Data is not null
            && control.Data.TryGetValue("code", out var code)
            && !string.IsNullOrWhiteSpace(code))
        {
            return code;
        }

        var label = control.Label ?? control.ControlId;
        return label.Length <= 24 ? label : label[..24];
    }

    private static string FormatSignalValue(string raw) =>
        string.IsNullOrWhiteSpace(raw) ? "?" : raw.Trim();

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
