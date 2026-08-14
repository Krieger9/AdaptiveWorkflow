using AdaptiveTeamBuilder.Data.Contracts;

namespace AdaptiveTeamBuilderSvc;

public interface ICollaborationProfileUpdater
{
    Task<CollaborationProfileUpdateResult> UpdateFromObservationsAsync(
        CollaborationTendencyBundleDto current,
        IReadOnlyList<CollaborationInteractionEventDto> events,
        CollaborationProfileUpdateContext? context = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Outcome of a profile update: the applied profile plus an optional natural-language reason
/// the AI gave for changing it (null when nothing meaningful changed).
/// </summary>
public sealed record CollaborationProfileUpdateResult(
    CollaborationTendencyBundleDto Profile,
    string? ChangeReason = null);

/// <summary>
/// Heuristic profile updater used when Foundry is not configured, and as a failure fallback.
/// </summary>
public sealed class StubCollaborationProfileUpdater(
    ICollaborationAgentTranscriptLogger transcripts) : ICollaborationProfileUpdater
{
    public async Task<CollaborationProfileUpdateResult> UpdateFromObservationsAsync(
        CollaborationTendencyBundleDto current,
        IReadOnlyList<CollaborationInteractionEventDto> events,
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
        return new CollaborationProfileUpdateResult(updated, reason);
    }

    /// <summary>
    /// Builds a concise reason from this batch's preference signals, or null when there are no
    /// signals worth recording (so the caller can skip logging a no-op change).
    /// </summary>
    public static string? SummarizeChangeReason(
        IReadOnlyList<CollaborationInteractionEventDto> events)
    {
        var observations = SummarizePreferenceSignals(events);
        return string.IsNullOrWhiteSpace(observations)
            ? null
            : "(Stub updater) Preference cues from latest Select Contract turn: " + observations;
    }

    public CollaborationTendencyBundleDto UpdateFromObservations(
        CollaborationTendencyBundleDto current,
        IReadOnlyList<CollaborationInteractionEventDto> events)
    {
        var observations = SummarizePreferenceSignals(events);
        var baseProse = string.IsNullOrWhiteSpace(current.UserOverride)
            ? current.AppDefaults
            : current.UserOverride!;

        string updated;
        if (string.IsNullOrWhiteSpace(observations))
        {
            updated =
                baseProse.Trim()
                + "\n\n(Stub updater) No new preference signals this batch.";
        }
        else
        {
            updated =
                baseProse.Trim()
                + "\n\n(Stub updater) Preference cues from latest Select Contract turn: "
                + observations;
        }

        return new CollaborationTendencyBundleDto(
            current.AppDefaults,
            updated,
            DateTime.UtcNow,
            "stub");
    }

    public static string BuildUpdatePrompt(
        CollaborationTendencyBundleDto current,
        IReadOnlyList<CollaborationInteractionEventDto> events,
        CollaborationProfileUpdateContext? context = null)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(
            "You maintain the user's durable collaboration profile for Select Contract.");
        sb.AppendLine(CollaborationContextFormatter.FormatExpectationGuidance());
        sb.AppendLine();
        sb.AppendLine(CollaborationContextFormatter.FormatRetrievedProfile(current));
        sb.AppendLine();
        sb.AppendLine(
            CollaborationContextFormatter.FormatRecentTurnDigests(context?.RecentTurnDigests));
        sb.AppendLine();

        if (context is not null)
        {
            if (!string.IsNullOrWhiteSpace(context.ScreenId))
            {
                sb.AppendLine(
                    $"Screen: {context.ScreenTitle ?? context.ScreenId} ({context.ScreenId})");
            }

            if (context.ScreenAnnotations is { Count: > 0 })
            {
                sb.AppendLine("Screen annotations:");
                foreach (var pair in context.ScreenAnnotations)
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
                "Recent semantic observations to incorporate into the profile:"));
        sb.AppendLine();
        sb.AppendLine(CollaborationContextFormatter.FormatActionTiming(events));
        sb.AppendLine();
        sb.AppendLine(
            "Return the full updated TendencyProse (activeSummary replacement). "
            + "Use recent digests + this turn to detect habit shifts: when digests contradict "
            + "activeSummary (≥2 agreeing new patterns, or clear CONTRADICTS flags), rewrite away "
            + "the old commercial-signal / compareStyle claim. "
            + "If activeSummary already matches this turn, you may strengthen it. "
            + "Use timing cues to down-weight accidental toggles. "
            + "Do not preserve contradicted preferences out of loyalty to prior prose. "
            + "When you actually change the profile, also return a concise changeReason "
            + "explaining why (e.g. 'User selected graph view 3 turns running; switching to "
            + "graph display'); leave changeReason null/empty when the profile is unchanged.");
        return sb.ToString().TrimEnd();
    }

    public static string SummarizePreferenceSignals(
        IReadOnlyList<CollaborationInteractionEventDto> events)
    {
        if (events.Count == 0)
        {
            return string.Empty;
        }

        var parts = new List<string>();

        var lastSignalsDisplay = events
            .Where(e => e.Type == "view.change"
                && string.Equals(
                    e.Meta?.GetValueOrDefault("preferenceAxis"),
                    "signalsDisplay",
                    StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.At)
            .Select(e => e.Meta?.GetValueOrDefault("to"))
            .LastOrDefault(v => !string.IsNullOrWhiteSpace(v));

        if (!string.IsNullOrWhiteSpace(lastSignalsDisplay))
        {
            parts.Add(
                lastSignalsDisplay.Equals("graph", StringComparison.OrdinalIgnoreCase)
                    ? "prefers relative graph signal display over numeric values"
                    : "prefers numeric values signal display over graphs");
        }

        var expanded = events.Count(e => e.Type == "control.expand");
        var collapsed = events.Count(e => e.Type == "control.collapse");
        if (expanded > collapsed)
        {
            parts.Add("leans toward extended card detail before deciding");
        }
        else if (collapsed > expanded)
        {
            parts.Add("leans toward summary cards without extended detail");
        }
        else if (expanded > 0)
        {
            parts.Add("toggles between summary and extended detail while comparing");
        }

        var signalIds = events
            .Where(e => e.Type is "signal.focus" or "signal.activate")
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
            .Where(e => e.Type == "control.select")
            .Select(e => e.Label ?? e.ControlId)
            .LastOrDefault();
        if (!string.IsNullOrWhiteSpace(selected))
        {
            parts.Add($"selected {selected}");
        }

        if (parts.Count == 0)
        {
            var meaningful = events.Count(e =>
                e.Type is not ("screen.enter" or "screen.leave"));
            return meaningful == 0
                ? string.Empty
                : $"recorded {meaningful} interaction(s) without a clear view-style cue.";
        }

        return string.Join("; ", parts) + ".";
    }
}
