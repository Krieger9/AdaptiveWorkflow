using System.Text.Json;
using AdaptiveTeamBuilder.Data.Contracts;
using Microsoft.Agents.AI;

namespace AdaptiveTeamBuilderSvc;

/// <summary>
/// Foundry-backed advisor. Falls back to <see cref="StubCollaborationAdvisor"/> on failure.
/// </summary>
public sealed class AgentCollaborationAdvisor(
    FoundryCollaborationAgents agents,
    StubCollaborationAdvisor fallback,
    ICollaborationAgentTranscriptLogger transcripts,
    ILogger<AgentCollaborationAdvisor> logger) : ICollaborationAdvisor
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<CollaborationAdviseResponse> AdviseAsync(
        CollaborationAdviseRequest request,
        CollaborationTendencyBundleDto profile,
        CancellationToken cancellationToken = default)
    {
        var promptPreview = StubCollaborationAdvisor.BuildPromptPreview(request, profile);
        var turnContext = new CollaborationProfileUpdateContext(
            request.Screen.ScreenId,
            request.Screen.Title,
            request.Screen.ViewState,
            request.Screen.Annotations,
            request.App.ContractCount,
            request.Controls);

        try
        {
            var agentResponse = await agents.Advisor.RunAsync<AdviseAgentResult>(
                promptPreview,
                session: null,
                serializerOptions: SerializerOptions,
                options: null,
                cancellationToken: cancellationToken);

            var suggestions = AdviseAgentResultMapper.ToSuggestions(agentResponse.Result);
            var preferredLayout = AdviseAgentResultMapper.ToPreferredLayout(agentResponse.Result);
            if (suggestions.Count == 0)
            {
                logger.LogWarning(
                    "Foundry advisor returned no applyable suggestions; using stub heuristics.");
                var stub = fallback.Advise(request, profile) with { PromptPreview = promptPreview };
                await transcripts.WriteAsync(
                    new CollaborationAgentTranscript
                    {
                        Agent = FoundryCollaborationAgents.AdvisorAgentName,
                        Source = "stub-fallback",
                        Prompt = promptPreview,
                        RetrievedProfile = profile,
                        TurnContext = turnContext,
                        Events = request.Events,
                        ResponseText = agentResponse.Text,
                        ResponseObject = new
                        {
                            foundryResult = agentResponse.Result,
                            preferredLayout = stub.PreferredLayout,
                            appliedSuggestions = stub.Suggestions,
                        },
                    },
                    cancellationToken);
                return stub;
            }

            preferredLayout = MergePreferredLayout(
                preferredLayout,
                StubCollaborationAdvisor.BuildPreferredLayout(request, profile));
            var response = new CollaborationAdviseResponse(
                promptPreview,
                suggestions,
                preferredLayout);
            await transcripts.WriteAsync(
                new CollaborationAgentTranscript
                {
                    Agent = FoundryCollaborationAgents.AdvisorAgentName,
                    Source = "foundry",
                    Prompt = promptPreview,
                    RetrievedProfile = profile,
                    TurnContext = turnContext,
                    Events = request.Events,
                    ResponseText = agentResponse.Text,
                    ResponseObject = new
                    {
                        foundryResult = agentResponse.Result,
                        preferredLayout,
                        suggestions,
                    },
                },
                cancellationToken);
            return response;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Foundry advisor failed; falling back to stub heuristics.");
            var stub = fallback.Advise(request, profile);
            await transcripts.WriteAsync(
                new CollaborationAgentTranscript
                {
                    Agent = FoundryCollaborationAgents.AdvisorAgentName,
                    Source = "error",
                    Prompt = promptPreview,
                    RetrievedProfile = profile,
                    TurnContext = turnContext,
                    Events = request.Events,
                    ResponseObject = new
                    {
                        preferredLayout = stub.PreferredLayout,
                        appliedSuggestions = stub.Suggestions,
                    },
                    Error = ex.ToString(),
                },
                cancellationToken);
            return stub;
        }
    }

    /// <summary>
    /// Prefer Foundry layout, but fill expandTopCount/expandBySignal from stub when the model
    /// understood keep-top-subset in prose yet omitted the structured fields (expandAll=false only).
    /// </summary>
    private static CollaborationPreferredLayoutDto MergePreferredLayout(
        CollaborationPreferredLayoutDto? fromAgent,
        CollaborationPreferredLayoutDto fromStub)
    {
        if (fromAgent is null)
        {
            return fromStub;
        }

        if (fromAgent.ExpandTopCount is not null && !string.IsNullOrWhiteSpace(fromAgent.ExpandBySignal))
        {
            return fromAgent with { ExpandAll = false };
        }

        if (fromStub.ExpandTopCount is not null && !string.IsNullOrWhiteSpace(fromStub.ExpandBySignal))
        {
            return fromAgent with
            {
                ExpandAll = false,
                ExpandTopCount = fromStub.ExpandTopCount,
                ExpandBySignal = fromStub.ExpandBySignal,
                Rationale = string.IsNullOrWhiteSpace(fromAgent.Rationale)
                    ? fromStub.Rationale
                    : fromAgent.Rationale
                      + " (filled expandTop from profile: "
                      + $"{fromStub.ExpandTopCount} by {fromStub.ExpandBySignal})",
            };
        }

        return fromAgent;
    }
}
