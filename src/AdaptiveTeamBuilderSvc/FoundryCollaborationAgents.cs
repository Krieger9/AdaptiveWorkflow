using System.ClientModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Responses;

namespace AdaptiveTeamBuilderSvc;

#pragma warning disable OPENAI001 // ResponsesClient is the supported Foundry path for this POC.

/// <summary>
/// Builds Microsoft Agent Framework agents against the Foundry OpenAI Responses endpoint.
/// </summary>
public sealed class FoundryCollaborationAgents
{
    public const string AdvisorAgentName = "CollaborationAdvisor";
    public const string ProfileUpdaterAgentName = "CollaborationProfileUpdater";

    public FoundryCollaborationAgents(IOptions<AgentFrameworkOptions> options)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
        {
            throw new InvalidOperationException(
                "AgentFramework is not configured. Set AgentFramework:ApiKey via User Secrets.");
        }

        var client = new ResponsesClient(
            credential: new ApiKeyCredential(settings.ApiKey!),
            options: new OpenAIClientOptions
            {
                Endpoint = new Uri(settings.OpenAIEndpoint),
            });

        Advisor = client.AsAIAgent(
            model: settings.DeploymentName,
            instructions:
                "You adapt the Select Contract UI to this user's preferred view styles. "
                + "Always start from the retrieved activeSummary profile. "
                + "Interpret the profile (and any semantic actions) for signalsDisplay "
                + "(values vs graph), compareStyle (expand-all-before-select vs keep-top-two vs "
                + "summary-first), and preferred commercial signal (Margin/Profit/Value/Win). "
                + "Always return preferredLayout: "
                + "expandAll=true only when the durable habit is to open every visible contract. "
                + "When the habit is keep-top-two / open highest by a signal / collapse lowest "
                + "(e.g. Prefers Margin; opens the two highest-margin cards), set expandAll=false, "
                + "expandTopCount=2, expandBySignal=Margin (or Profit/Value/Win prob.) — do not "
                + "leave expandTopCount null in that case. "
                + "When the habit is expand-one / open only the highest by a signal "
                + "(e.g. Prefers Profit; keeps only the highest-profit card expanded), set "
                + "expandAll=false, expandTopCount=1, expandBySignal=Profit. "
                + "expandAll=false with null expandTopCount only for true summary-first. "
                + "Subtle language counts — do not rely on naive keyword matching. Boilerplate "
                + "'start with summary' must not override a clear expand-all or keep-top-N habit. "
                + "Set preferredLayout.signalsDisplay to values or graph when clear, else null. "
                + "Also return suggestions for interactive adaptations: kind=set-view with "
                + "payload.signalsDisplay=values|graph, or kind=expand|collapse|select with "
                + "targetControlId. Prefer at most 1-3 concrete suggestions. Do not invent "
                + "control IDs. preferredLayout is auto-applied on load; suggestions are optional Accept actions.",
            name: AdvisorAgentName,
            description: "Suggests applyable Select Contract UI adaptations from semantic context.");

        ProfileUpdater = client.AsAIAgent(
            model: settings.DeploymentName,
            instructions:
                "You maintain a short natural-language user collaboration profile for Select Contract. "
                + "Start from the retrieved profile AND the recent decision-turn digests (last ~5). "
                + "Update TendencyProse from digests + this turn's semantic observations, "
                + "expandedCoverage, comparison pattern cues, commercial signal rank cues "
                + "(keep-top-2 / collapse-lowest / expand-one+select-high / cross-ranks), and timing. "
                + "Habit shifts are bidirectional: expand-one ↔ keep-top-two and Margin ↔ Profit. "
                + "If digests show keep-top-two-then-select / expanded=2/3 with Profit #1+#2 while "
                + "activeSummary still says expand-one by Profit, UNDERMINE expand-one and COMMIT "
                + "keep-top-two (expandTopCount=2) after ≥2 agreeing digests — do not re-assert "
                + "expand-one out of loyalty to prior prose. "
                + "expanded=2/3 counts as keep-top-two even with zero collapses. "
                + "After one contradiction mark the old habit under review; after ≥2 agreeing "
                + "digests on the new pattern/signal, COMMIT and remove the contradicted claim. "
                + "Keep the profile concise; rewrite noisy stub appends into clean preferences. "
                + "Preserve only preferences still supported by recent digests + this turn. "
                + "Return TendencyProse (the full updated profile text) and, whenever you "
                + "actually change the profile, a concise changeReason stating why "
                + "(e.g. 'User selected graph view 3 turns running; switching to graph display'). "
                + "Leave changeReason null/empty when the profile is unchanged.",
            name: ProfileUpdaterAgentName,
            description: "Updates user collaboration tendency prose from observation batches.");
    }

    public AIAgent Advisor { get; }

    public AIAgent ProfileUpdater { get; }
}

#pragma warning restore OPENAI001
