using AdaptiveTeamBuilder.Data.Contracts;

namespace AdaptiveTeamBuilderSvc;

/// <summary>
/// Decision point between "the agent produced a preferred layout / suggestions" and
/// "the UI applies them". Round 2 ships <see cref="AutoApproveAdaptationPolicy"/>, which
/// approves everything and logs the decision, preserving today's auto-apply behavior.
/// Switching to interactive Yes/No/"Not quite" later is a policy swap plus one UI card.
/// </summary>
public interface IAdaptationApprovalPolicy
{
    /// <summary>Name recorded on every decision, e.g. "auto-approve".</summary>
    string PolicyName { get; }

    Task<IReadOnlyList<AdaptationApprovalRecord>> DecideAsync(
        Guid userId,
        PreferredLayoutDto? preferredLayout,
        IReadOnlyList<SuggestionDto> suggestions,
        CancellationToken cancellationToken);
}

public sealed class AutoApproveAdaptationPolicy(
    ILogger<AutoApproveAdaptationPolicy> logger) : IAdaptationApprovalPolicy
{
    public string PolicyName => "auto-approve";

    public Task<IReadOnlyList<AdaptationApprovalRecord>> DecideAsync(
        Guid userId,
        PreferredLayoutDto? preferredLayout,
        IReadOnlyList<SuggestionDto> suggestions,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var decisions = new List<AdaptationApprovalRecord>();

        if (preferredLayout is not null)
        {
            decisions.Add(new AdaptationApprovalRecord
            {
                AdaptationId = "preferred-layout",
                AdaptationKind = "preferred-layout",
                Approved = true,
                Policy = PolicyName,
                Belief = DescribeLayoutBelief(preferredLayout),
                Rationale = preferredLayout.Rationale,
                DecidedAt = now,
            });
        }

        foreach (var suggestion in suggestions)
        {
            decisions.Add(new AdaptationApprovalRecord
            {
                AdaptationId = suggestion.Id,
                AdaptationKind = "suggestion",
                Approved = true,
                Policy = PolicyName,
                Belief = suggestion.Dimension,
                Rationale = suggestion.Rationale ?? suggestion.Label,
                DecidedAt = now,
            });
        }

        if (decisions.Count > 0)
        {
            logger.LogDebug(
                "Auto-approved {Count} adaptation(s) for user {UserId}.",
                decisions.Count,
                userId);
        }

        return Task.FromResult<IReadOnlyList<AdaptationApprovalRecord>>(decisions);
    }

    private static string DescribeLayoutBelief(PreferredLayoutDto layout)
    {
        if (layout.ExpandTopCount is { } top && !string.IsNullOrWhiteSpace(layout.ExpandBySignal))
        {
            return $"disclosure-default: selective (top {top} by {layout.ExpandBySignal})";
        }

        if (layout.ExpandAll)
        {
            return "disclosure-default: expanded";
        }

        return layout.SignalsDisplay is { Length: > 0 } display
            ? $"information-form: {(display == "graph" ? "charted" : "bare")}"
            : "disclosure-default: collapsed";
    }
}
