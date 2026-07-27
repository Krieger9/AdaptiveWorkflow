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
                + "(values vs graph) and compareStyle (expand-all-before-select vs summary-first). "
                + "Always return preferredLayout: set expandAll true when the durable habit is to "
                + "compare all/most visible contracts in extended detail before choosing; set "
                + "expandAll false for summary-first / choose-from-summary / don't-force habits. "
                + "Subtle language counts — do not rely on naive keyword matching. Boilerplate "
                + "'start with summary' must not override a clear expand-all-before-select habit. "
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
                + "Start from the retrieved profile (source/appDefaults/userOverride/activeSummary). "
                + "Update TendencyProse from semantic observations, expandedCoverage, comparison "
                + "pattern cues, and action timing about signalsDisplay (values vs graph), "
                + "detailLevel (summary vs extended), and compareStyle "
                + "(expand-one vs expand-all-before-select). "
                + "If the user expands all/most visible contracts then selects, record that they like "
                + "to open and analyze details on all contracts before choosing. "
                + "Use timing to down-weight accidental toggles (quick expand→collapse / likely-mistake) "
                + "and trust deliberate-dwell gaps more when inferring durable preferences. "
                + "Keep the profile concise and durable; rewrite noisy stub append logs into clean "
                + "preferences. Preserve useful prior preferences unless this turn clearly contradicts them. "
                + "Return only TendencyProse — the full updated profile text.",
            name: ProfileUpdaterAgentName,
            description: "Updates user collaboration tendency prose from observation batches.");
    }

    public AIAgent Advisor { get; }

    public AIAgent ProfileUpdater { get; }
}

#pragma warning restore OPENAI001
