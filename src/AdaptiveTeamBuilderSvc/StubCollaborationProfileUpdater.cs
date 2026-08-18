using AdaptiveTeamBuilder.Data.Contracts;

namespace AdaptiveTeamBuilderSvc;

public interface ICollaborationProfileUpdater
{
    Task<CollaborationProfileUpdateResult> UpdateFromObservationsAsync(
        BeliefProfileDto current,
        IReadOnlyList<InteractionDto> events,
        CollaborationProfileUpdateContext? context = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Outcome of a profile update: the applied belief document, an optional natural-language
/// change reason (null when nothing meaningful changed), how validation went
/// (ok | retried | rejected), and the raw request/response for the run record.
/// </summary>
public sealed record CollaborationProfileUpdateResult(
    BeliefProfileDto Profile,
    string? ChangeReason = null,
    string? ValidationResult = "ok",
    string? RawRequest = null,
    string? RawResponse = null,
    ProfileUpdateDiagnosticRecord? Diagnostics = null);

/// <summary>
/// Development/test utility for deterministic profile-update scenarios. Runtime composition does
/// not register this implementation; production failures keep the current profile unchanged.
/// </summary>
public sealed class StubCollaborationProfileUpdater(
    ICollaborationAgentTranscriptLogger transcripts) : ICollaborationProfileUpdater
{
    public async Task<CollaborationProfileUpdateResult> UpdateFromObservationsAsync(
        BeliefProfileDto current,
        IReadOnlyList<InteractionDto> events,
        CollaborationProfileUpdateContext? context = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var prompt = BuildUpdatePrompt(current, events, context);
        var updated = UpdateFromObservations(current, events);
        var reason = SummarizeChangeReason(events);
        await transcripts.WriteAsync(
            new CollaborationAgentTranscript
            {
                Agent = FoundryCollaborationAgents.ProfileUpdaterAgentName,
                Source = "stub",
                Prompt = prompt,
                RetrievedProfile = current,
                TurnContext = context,
                Events = events,
                ResponseObject = new { appliedProfile = updated, changeReason = reason },
            },
            cancellationToken);
        return new CollaborationProfileUpdateResult(
            updated,
            reason,
            ValidationResult: "ok",
            RawRequest: prompt,
            RawResponse: updated.Document);
    }

    /// <summary>
    /// Builds a concise reason from this batch's preference signals, or null when there are no
    /// signals worth recording (so the caller can skip logging a no-op change).
    /// </summary>
    public static string? SummarizeChangeReason(
        IReadOnlyList<InteractionDto> events)
    {
        var observations = SummarizePreferenceSignals(UserEvidence(events));
        return string.IsNullOrWhiteSpace(observations)
            ? null
            : "(Stub updater) Preference cues from latest turn: " + observations;
    }

    /// <summary>
    /// Parse-modify-write over the belief document: rewrites the dimension statements the
    /// evidence touches and appends a changelog entry. Only causation=user interactions
    /// count as evidence.
    /// </summary>
    public BeliefProfileDto UpdateFromObservations(
        BeliefProfileDto current,
        IReadOnlyList<InteractionDto> events)
    {
        var evidence = UserEvidence(events);
        var observations = SummarizePreferenceSignals(evidence);
        if (string.IsNullOrWhiteSpace(observations))
        {
            return current with { Source = "stub" };
        }

        var scope = BeliefDocumentFormat.ContractsListScope;
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var document = current.Document;
        var touched = new List<string>();

        var lastSignalsDisplay = LastSignalsDisplay(evidence);
        var reversed = evidence.Any(e => e.Reversal == true);
        if (!string.IsNullOrWhiteSpace(lastSignalsDisplay))
        {
            var statement = lastSignalsDisplay.Equals("graph", StringComparison.OrdinalIgnoreCase)
                ? "Prefers relative graph signal display over numeric values."
                : "Prefers numeric signal values over graphs.";
            document = SetBelief(
                document,
                scope,
                "information-form",
                statement,
                reversed ? "working theory" : "tentative",
                date,
                "The user switched the signals display themselves this turn"
                + (reversed ? ", reversing an agent-applied display" : "") + ".");
            touched.Add("information-form");
        }

        var expanded = evidence.Count(e => e.Action == "control.expand");
        var collapsed = evidence.Count(e => e.Action == "control.collapse");
        if (expanded != collapsed || expanded > 0)
        {
            var statement = expanded > collapsed
                ? "Leans toward extended card detail before deciding."
                : collapsed > expanded
                    ? "Leans toward summary cards, collapsing extra detail."
                    : "Toggles between summary and extended detail while comparing.";
            document = SetBelief(
                document,
                scope,
                "disclosure-default",
                statement,
                "tentative",
                date,
                $"Expand/collapse balance this turn: {expanded} expand(s), {collapsed} collapse(s).");
            touched.Add("disclosure-default");
        }

        var signalIds = evidence
            .Where(e => e.Action is "signal.focus" or "signal.activate")
            .Select(e => e.Meta?.GetValueOrDefault("signalId") ?? e.Label)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToList();
        if (signalIds.Count > 0)
        {
            document = SetBelief(
                document,
                scope,
                "metric-attention",
                "Inspects these signals first: " + string.Join(", ", signalIds) + ".",
                "tentative",
                date,
                "Signal focus/activate interactions this turn.");
            touched.Add("metric-attention");
        }

        var selection = evidence.LastOrDefault(e => e.Action == "control.select");
        if (selection is not null)
        {
            var alternatives = selection.ChoiceSet is { Count: > 1 }
                ? $" from a visible choice set of {selection.ChoiceSet.Count}"
                : string.Empty;
            document = SetBelief(
                document,
                scope,
                "selection-rule",
                $"Most recently selected {selection.Label ?? selection.ControlId}{alternatives}. "
                + "No committed rule yet for which contracts are inspected first.",
                "noticed",
                date,
                "A single selection is not yet a rule; watching for a repeated pattern over the choice set.");
            touched.Add("selection-rule");
        }

        if (touched.Count == 0)
        {
            return current with { Source = "stub" };
        }

        document = BeliefDocumentFormat.AppendChangelogEntry(
            document,
            $"{date} · revised {string.Join(", ", touched)}",
            "(Stub updater) Preference cues from latest turn: " + observations);

        return current with { Document = document, Source = "stub" };
    }

    private static string SetBelief(
        string document,
        string scope,
        string dimension,
        string statement,
        string conviction,
        string date,
        string leaningOn)
    {
        document = BeliefDocumentFormat.ReplaceBeliefField(document, scope, dimension, "Belief", statement);
        document = BeliefDocumentFormat.ReplaceBeliefField(document, scope, dimension, "Conviction", conviction);
        document = BeliefDocumentFormat.ReplaceBeliefField(
            document,
            scope,
            dimension,
            "Tenure",
            $"updated {date} by stub observation");
        return BeliefDocumentFormat.ReplaceBeliefField(
            document,
            scope,
            dimension,
            "What I'm leaning on",
            leaningOn);
    }

    /// <summary>Only causation=user interactions are evidence (§ causation rules).</summary>
    public static IReadOnlyList<InteractionDto> UserEvidence(
        IReadOnlyList<InteractionDto> events) =>
        events
            .Where(e => string.IsNullOrWhiteSpace(e.Causation)
                || string.Equals(e.Causation, "user", StringComparison.OrdinalIgnoreCase))
            .ToList();

    private static string? LastSignalsDisplay(IReadOnlyList<InteractionDto> evidence) =>
        evidence
            .Where(e => e.Action == "view.change"
                && string.Equals(
                    e.Meta?.GetValueOrDefault("preferenceAxis"),
                    "signalsDisplay",
                    StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.At)
            .Select(e => e.Meta?.GetValueOrDefault("to"))
            .LastOrDefault(v => !string.IsNullOrWhiteSpace(v));

    public static string BuildUpdatePrompt(
        BeliefProfileDto current,
        IReadOnlyList<InteractionDto> events,
        CollaborationProfileUpdateContext? context = null)
    {
        var sb = new System.Text.StringBuilder();
        if (!string.IsNullOrWhiteSpace(context?.PromptOverride))
        {
            // Replay-with-modified-prompt: the override replaces the standing guidance block.
            sb.AppendLine(context.PromptOverride.Trim());
        }
        else
        {
            sb.AppendLine(
                "You maintain this user's belief document for the surfaces below.");
            sb.AppendLine(CollaborationContextFormatter.FormatExpectationGuidance());
        }
        sb.AppendLine();
        sb.AppendLine(CollaborationContextFormatter.FormatRetrievedProfile(current));
        sb.AppendLine();
        sb.AppendLine(
            CollaborationContextFormatter.FormatRecentTurnDigests(context?.RecentTurnDigests));
        sb.AppendLine();

        if (context is not null)
        {
            if (!string.IsNullOrWhiteSpace(context.AssembledContext))
            {
                sb.AppendLine("Assembled surface context:");
                sb.AppendLine(context.AssembledContext.Trim());
            }
            else if (!string.IsNullOrWhiteSpace(context.SurfacePath))
            {
                sb.AppendLine(
                    $"Surface: {context.SurfaceTitle ?? context.SurfacePath} ({context.SurfacePath})");
            }

            if (context.SurfaceAnnotations is { Count: > 0 })
            {
                sb.AppendLine("Surface annotations:");
                foreach (var pair in context.SurfaceAnnotations)
                {
                    sb.AppendLine($"  {pair.Key}: {pair.Value}");
                }
            }

            if (context.ViewState is not null)
            {
                sb.AppendLine(
                    CollaborationContextFormatter.FormatViewState(
                        context.ViewState,
                        context.VisibleControlCount));
                sb.AppendLine(
                    CollaborationContextFormatter.FormatComparisonPattern(
                        context.ViewState,
                        context.VisibleControlCount,
                        events));
                sb.AppendLine(
                    CollaborationContextFormatter.FormatSignalRankComparison(
                        context.Controls,
                        context.ViewState,
                        events));
            }

            sb.AppendLine();
        }

        sb.AppendLine(
            CollaborationContextFormatter.FormatSemanticActions(
                events,
                "Interactions to incorporate into the belief document:"));
        sb.AppendLine();
        sb.AppendLine(CollaborationContextFormatter.FormatActionTiming(events));
        sb.AppendLine();
        sb.AppendLine(
            "Return the COMPLETE updated belief document (all sections, all fields, plus the "
            + "changelog with your new entry appended). Every belief section must keep all five "
            + "fields: Belief, Tenure, Conviction, What I'm leaning on, What would change my mind. "
            + "Conviction must be one of: noticed, tentative, working theory, settled, entrenched. "
            + "Append at least one changelog entry stating what happened (revised / challenged / "
            + "confirmed / refreshed / created / retired / proposed) and why. Use recent digests "
            + "and this turn to detect habit shifts: when digests contradict a held belief (≥2 "
            + "agreeing new patterns, or "
            + "clear CONTRADICTS flags), revise the old commercial-signal / selection-rule claim. "
            + "If a belief already matches this turn, you may raise its conviction one level. "
            + "Use timing cues to discount accidental toggles. Do not preserve contradicted "
            + "beliefs out of loyalty to prior prose. Also return a concise changeReason when you "
            + "actually change a belief (e.g. 'User selected graph view 3 turns running; revising "
            + "information-form to graph'); leave changeReason null/empty when nothing changed.");
        return sb.ToString().TrimEnd();
    }

    public static string SummarizePreferenceSignals(
        IReadOnlyList<InteractionDto> events)
    {
        if (events.Count == 0)
        {
            return string.Empty;
        }

        var parts = new List<string>();

        var lastSignalsDisplay = LastSignalsDisplay(events);
        if (!string.IsNullOrWhiteSpace(lastSignalsDisplay))
        {
            parts.Add(
                lastSignalsDisplay.Equals("graph", StringComparison.OrdinalIgnoreCase)
                    ? "prefers relative graph signal display over numeric values"
                    : "prefers numeric signal values over graphs");
        }

        var expanded = events.Count(e => e.Action == "control.expand");
        var collapsed = events.Count(e => e.Action == "control.collapse");
        if (expanded > collapsed)
        {
            parts.Add("leans toward extended card detail before deciding");
        }
        else if (collapsed > expanded)
        {
            parts.Add("leans toward summary cards without opening every card");
        }
        else if (expanded > 0)
        {
            parts.Add("toggles between summary and extended detail while comparing");
        }

        var signalIds = events
            .Where(e => e.Action is "signal.focus" or "signal.activate")
            .Select(e => e.Meta?.GetValueOrDefault("signalId") ?? e.Label)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToList();
        if (signalIds.Count > 0)
        {
            parts.Add("inspects signals: " + string.Join(", ", signalIds));
        }

        var selected = events
            .Where(e => e.Action == "control.select")
            .Select(e => e.Label ?? e.ControlId)
            .LastOrDefault();
        if (!string.IsNullOrWhiteSpace(selected))
        {
            parts.Add($"selected {selected}");
        }

        if (events.Any(e => e.Reversal == true))
        {
            parts.Add("REVERSED an agent-applied state (strongest signal)");
        }

        if (parts.Count == 0)
        {
            var meaningful = events.Count(e =>
                e.Action is not ("surface.enter" or "surface.leave" or "screen.enter" or "screen.leave"));
            return meaningful == 0
                ? string.Empty
                : $"recorded {meaningful} interaction(s) without a clear view-style cue.";
        }

        return string.Join("; ", parts) + ".";
    }
}
