using System.Text.Json;
using AdaptiveTeamBuilder.Data.Contracts;
using Microsoft.Agents.AI;

namespace AdaptiveTeamBuilderSvc;

/// <summary>
/// Foundry-backed advisor. When Foundry cannot provide advice, the client keeps its default view.
/// </summary>
public sealed class AgentCollaborationAdvisor(
    FoundryCollaborationAgents agents,
    ICollaborationAgentTranscriptLogger transcripts,
    ILogger<AgentCollaborationAdvisor> logger) : ICollaborationAdvisor
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<AdviseResponse> AdviseAsync(
        AdviseRequest request,
        BeliefProfileDto profile,
        CancellationToken cancellationToken = default)
    {
        var promptPreview = StubCollaborationAdvisor.BuildPromptPreview(request, profile);
        var turnContext = new CollaborationProfileUpdateContext(
            string.Join(" / ", request.Surface.SurfacePath),
            request.Surface.Title,
            request.Surface.ViewState,
            request.Surface.Annotations,
            request.App.ItemCount,
            request.Controls);

        try
        {
            var agentResponse = await agents.Advisor.RunAsync<AdviseAgentResult>(
                promptPreview,
                session: null,
                serializerOptions: SerializerOptions,
                options: agents.CreateRunOptions(),
                cancellationToken: cancellationToken);

            var suggestions = AdviseAgentResultMapper.ToSuggestions(agentResponse.Result);
            var preferredLayout = AdviseAgentResultMapper.ToPreferredLayout(agentResponse.Result);
            if (suggestions.Count == 0)
            {
                logger.LogInformation(
                    "Foundry advisor returned no applicable suggestions; keeping the client default view.");
                await transcripts.WriteAsync(
                    new CollaborationAgentTranscript
                    {
                        Agent = FoundryCollaborationAgents.AdvisorAgentName,
                        Source = "foundry",
                        Prompt = promptPreview,
                        RetrievedProfile = profile,
                        TurnContext = turnContext,
                        Events = request.Interactions,
                        ResponseText = agentResponse.Text,
                        ResponseObject = new
                        {
                            foundryResult = agentResponse.Result,
                            preferredLayout = (PreferredLayoutDto?)null,
                            suggestions = Array.Empty<SuggestionDto>(),
                        },
                    },
                    cancellationToken);
                return new AdviseResponse(
                    promptPreview,
                    Array.Empty<SuggestionDto>(),
                    PreferredLayout: null);
            }

            var response = new AdviseResponse(
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
                    Events = request.Interactions,
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
            logger.LogError(ex, "Foundry advisor failed; keeping the client default view.");
            await transcripts.WriteAsync(
                new CollaborationAgentTranscript
                {
                    Agent = FoundryCollaborationAgents.AdvisorAgentName,
                    Source = "error",
                    Prompt = promptPreview,
                    RetrievedProfile = profile,
                    TurnContext = turnContext,
                    Events = request.Interactions,
                    ResponseObject = new
                    {
                        preferredLayout = (PreferredLayoutDto?)null,
                        suggestions = Array.Empty<SuggestionDto>(),
                    },
                    Error = ex.ToString(),
                },
                cancellationToken);
            return new AdviseResponse(
                promptPreview,
                Array.Empty<SuggestionDto>(),
                PreferredLayout: null);
        }
    }
}
