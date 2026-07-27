using System.Text.Json;
using AdaptiveTeamBuilder.Data.Contracts;
using Microsoft.Agents.AI;

namespace AdaptiveTeamBuilderSvc;

/// <summary>
/// Foundry-backed profile updater. Falls back to <see cref="StubCollaborationProfileUpdater"/> on failure.
/// </summary>
public sealed class AgentCollaborationProfileUpdater(
    FoundryCollaborationAgents agents,
    StubCollaborationProfileUpdater fallback,
    ICollaborationAgentTranscriptLogger transcripts,
    ILogger<AgentCollaborationProfileUpdater> logger) : ICollaborationProfileUpdater
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<CollaborationTendencyBundleDto> UpdateFromObservationsAsync(
        CollaborationTendencyBundleDto current,
        IReadOnlyList<CollaborationInteractionEventDto> events,
        CollaborationProfileUpdateContext? context = null,
        CancellationToken cancellationToken = default)
    {
        if (events.Count == 0)
        {
            return current;
        }

        var prompt = StubCollaborationProfileUpdater.BuildUpdatePrompt(current, events, context);

        try
        {
            var agentResponse = await agents.ProfileUpdater.RunAsync<ProfileUpdateAgentResult>(
                prompt,
                session: null,
                serializerOptions: SerializerOptions,
                options: null,
                cancellationToken: cancellationToken);

            var prose = agentResponse.Result?.TendencyProse?.Trim();
            if (string.IsNullOrWhiteSpace(prose))
            {
                logger.LogWarning(
                    "Foundry profile updater returned empty TendencyProse; using stub updater.");
                var stub = fallback.UpdateFromObservations(current, events);
                await transcripts.WriteAsync(
                    new CollaborationAgentTranscript
                    {
                        Agent = FoundryCollaborationAgents.ProfileUpdaterAgentName,
                        Source = "stub-fallback",
                        Prompt = prompt,
                        RetrievedProfile = current,
                        TurnContext = context,
                        Events = events,
                        ResponseText = agentResponse.Text,
                        ResponseObject = new
                        {
                            foundryResult = agentResponse.Result,
                            appliedProfile = stub,
                        },
                    },
                    cancellationToken);
                return stub;
            }

            var updated = new CollaborationTendencyBundleDto(
                current.AppDefaults,
                prose,
                DateTime.UtcNow,
                "llm");

            await transcripts.WriteAsync(
                new CollaborationAgentTranscript
                {
                    Agent = FoundryCollaborationAgents.ProfileUpdaterAgentName,
                    Source = "foundry",
                    Prompt = prompt,
                    RetrievedProfile = current,
                    TurnContext = context,
                    Events = events,
                    ResponseText = agentResponse.Text,
                    ResponseObject = new
                    {
                        foundryResult = agentResponse.Result,
                        appliedProfile = updated,
                    },
                },
                cancellationToken);

            return updated;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Foundry profile updater failed; falling back to stub updater.");
            var stub = fallback.UpdateFromObservations(current, events);
            await transcripts.WriteAsync(
                new CollaborationAgentTranscript
                {
                    Agent = FoundryCollaborationAgents.ProfileUpdaterAgentName,
                    Source = "error",
                    Prompt = prompt,
                    RetrievedProfile = current,
                    TurnContext = context,
                    Events = events,
                    ResponseObject = new { appliedProfile = stub },
                    Error = ex.ToString(),
                },
                cancellationToken);
            return stub;
        }
    }
}
