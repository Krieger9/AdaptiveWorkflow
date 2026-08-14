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

    public async Task<CollaborationProfileUpdateResult> UpdateFromObservationsAsync(
        CollaborationTendencyBundleDto current,
        IReadOnlyList<CollaborationInteractionEventDto> events,
        CollaborationProfileUpdateContext? context = null,
        CancellationToken cancellationToken = default)
    {
        if (events.Count == 0)
        {
            return new CollaborationProfileUpdateResult(current);
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
                var stubReason = StubCollaborationProfileUpdater.SummarizeChangeReason(events);
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
                            changeReason = stubReason,
                        },
                    },
                    cancellationToken);
                return new CollaborationProfileUpdateResult(stub, stubReason);
            }

            var updated = new CollaborationTendencyBundleDto(
                current.AppDefaults,
                prose,
                DateTime.UtcNow,
                "llm");
            var changeReason = agentResponse.Result?.ChangeReason?.Trim();

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

            return new CollaborationProfileUpdateResult(
                updated,
                string.IsNullOrWhiteSpace(changeReason) ? null : changeReason);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Foundry profile updater failed; falling back to stub updater.");
            var stub = fallback.UpdateFromObservations(current, events);
            var stubReason = StubCollaborationProfileUpdater.SummarizeChangeReason(events);
            await transcripts.WriteAsync(
                new CollaborationAgentTranscript
                {
                    Agent = FoundryCollaborationAgents.ProfileUpdaterAgentName,
                    Source = "error",
                    Prompt = prompt,
                    RetrievedProfile = current,
                    TurnContext = context,
                    Events = events,
                    ResponseObject = new { appliedProfile = stub, changeReason = stubReason },
                    Error = ex.ToString(),
                },
                cancellationToken);
            return new CollaborationProfileUpdateResult(stub, stubReason);
        }
    }
}
