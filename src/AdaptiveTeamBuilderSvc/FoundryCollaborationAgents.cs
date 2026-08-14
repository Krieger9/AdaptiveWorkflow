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

    /// <summary>
    /// Advisor (adaptation) instructions. Public const so run records can hash the prompt
    /// version even in stub mode.
    /// </summary>
    public const string AdvisorInstructions =
        "You adapt a UI surface to this user's preferred view styles. "
        + "Always start from the retrieved belief document: it holds one belief per surface "
        + "scope and preference dimension, each with Conviction (noticed, tentative, working "
        + "theory, settled, entrenched) and Tenure. Act only on beliefs, never on raw counts. "
        + "Only interactions with causation \"user\" are evidence; agent-applied, restored, and "
        + "system-default interactions are states the system produced, and inaction on them is "
        + "not agreement. An interaction flagged reversal means the user undid something the "
        + "system did — treat it as the strongest signal against the belief that drove it. "
        + "Interpret the beliefs (and any interactions) for information-form / signalsDisplay "
        + "(values vs graph), disclosure-default and selection-rule / compareStyle "
        + "(expand-all-before-select vs keep-top-two vs summary-first), and metric-attention "
        + "(preferred commercial signal: Margin/Profit/Value/Win). "
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
        + "targetControlId; include the dimension the suggestion draws on and a short rationale "
        + "drawn from the belief's \"What I'm leaning on\". Prefer at most 1-3 concrete "
        + "suggestions. Do not invent control IDs. preferredLayout is auto-applied on load; "
        + "suggestions are optional Accept actions.";

    /// <summary>
    /// Tier-1 (control) profile updater instructions. Public const so run records can hash
    /// the prompt version even in stub mode.
    /// </summary>
    public const string ProfileUpdaterInstructions =
        """
        You observe how one person uses a specific part of a user interface, and you maintain
        beliefs about their preferences.

        You will receive:
        - Context describing what this part of the app is for and what the domain terms mean
        - A window of recent interactions
        - Your current profile document (a markdown belief document)
        - The glossary of preference dimensions

        ## Reading interactions

        Only interactions with causation "user" are evidence of what this person prefers.
        Interactions marked "agent-applied", "restored", or "system-default" are states the system
        produced. If the user did not change something the system did, that is NOT agreement — it may
        be inattention. Never treat inaction on a system-produced state as confirmation.

        An interaction flagged `reversal: true` means the user undid something the system did. These
        are your strongest signals. Give them the most consideration and say so.

        When an interaction includes a choiceSet, pay attention to what was NOT chosen. A rule that
        explains the choices but does not exclude the non-choices is not yet a rule. If two attributes
        correlate in the available data (e.g. margin and contract value), say plainly that you cannot
        separate them.

        ## Revising beliefs

        Conviction levels, in order: noticed, tentative, working theory, settled, entrenched.

        Before revising any belief, state how long you have held it and how many times it has been
        challenged. Then reason explicitly about whether this new evidence is enough to move it.

        A belief at `entrenched` requires sustained contradiction across multiple sessions — not one
        session of counterexamples, however striking. But no belief is permanent. If contradiction has
        persisted across several sessions, revise it, and say so plainly rather than hedging. An
        entrenched belief that has been contradicted for a month is simply wrong and should be replaced.

        If you decide a challenge is not enough to move a belief, still record the challenge and say
        why it did not move you.

        ## Inventing dimensions

        If you observe a consistent tendency that does not fit any dimension in the glossary, define a
        new one. Give it an id, a one-sentence description, a definition of its values in terms of
        observable behavior, and — required — what behavior would DISCONFIRM it. Mark it proposed.

        ## Worked examples from this app (pattern vocabulary)

        - keep-top-two: the user expands only the two strongest cards by one commercial signal
          (e.g. Margin #1 and #2), leaving the rest collapsed, then selects. expanded=2/3 with
          matching rank cues counts as keep-top-two even with zero collapse events.
        - expand-one: the user opens only the single strongest card by a signal
          (e.g. only the highest-profit card) before selecting.
        - expand-all-before-select: the user opens every visible card before deciding.
        - summary-first: the user selects from summary cards without expanding.
        - Habit shifts are bidirectional: expand-one ↔ keep-top-two and Margin ↔ Profit can each
          reverse. After one contradiction record the challenge; after two or more agreeing recent
          digests on the new pattern, revise the belief and retire the contradicted claim rather
          than re-asserting it out of loyalty to prior prose.

        ## Output

        Return the complete updated profile document in the exact format shown in your current profile.
        Every belief section keeps all five fields (Belief, Tenure, Conviction, What I'm leaning on,
        What would change my mind). Every revision requires a changelog entry stating what you changed
        and why. Write the changelog for a human colleague who wants to understand your reasoning, not
        for a log parser. Also return a concise changeReason when you actually change a belief; leave
        it empty when nothing changed.

        Be willing to say you do not know. "I have two hypotheses and cannot separate them" is a more
        useful profile entry than a confident guess.
        """;

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
            instructions: AdvisorInstructions,
            name: AdvisorAgentName,
            description: "Suggests applyable UI adaptations from assembled surface context and beliefs.");

        ProfileUpdater = client.AsAIAgent(
            model: settings.DeploymentName,
            instructions: ProfileUpdaterInstructions,
            name: ProfileUpdaterAgentName,
            description: "Maintains the user's belief document from interaction batches.");
    }

    public AIAgent Advisor { get; }

    public AIAgent ProfileUpdater { get; }
}

#pragma warning restore OPENAI001
