using System.Text.Json;
using AdaptiveTeamBuilder.Data.Contracts;
using Microsoft.Agents.AI;

namespace AdaptiveTeamBuilderSvc;

/// <summary>
/// Foundry-backed profile updater. Validates the returned belief document, retries once with
/// the validation errors appended, keeps the prior document when the retry also fails, and
/// falls back to <see cref="StubCollaborationProfileUpdater"/> on transport failure.
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
        BeliefProfileDto current,
        IReadOnlyList<InteractionDto> events,
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

            var document = agentResponse.Result?.ProfileDocument?.Trim();
            if (string.IsNullOrWhiteSpace(document))
            {
                logger.LogWarning(
                    "Foundry profile updater returned an empty document; using stub updater.");
                return await FallbackAsync(
                    current,
                    events,
                    context,
                    prompt,
                    "stub-fallback",
                    agentResponse.Text,
                    error: null,
                    cancellationToken);
            }

            var validationResult = "ok";
            var validation = BeliefDocumentFormat.Validate(document);
            if (!validation.IsValid)
            {
                // Retry once with the validation errors appended to the prompt.
                var retryPrompt =
                    prompt
                    + "\n\nYour previous document failed validation. Fix these issues and return "
                    + "the COMPLETE corrected document:\n- "
                    + string.Join("\n- ", validation.Errors);
                var retryResponse = await agents.ProfileUpdater.RunAsync<ProfileUpdateAgentResult>(
                    retryPrompt,
                    session: null,
                    serializerOptions: SerializerOptions,
                    options: null,
                    cancellationToken: cancellationToken);

                var retryDocument = retryResponse.Result?.ProfileDocument?.Trim();
                var retryValidation = string.IsNullOrWhiteSpace(retryDocument)
                    ? null
                    : BeliefDocumentFormat.Validate(retryDocument);
                if (retryValidation is { IsValid: true })
                {
                    document = retryDocument;
                    validationResult = "retried";
                    agentResponse = retryResponse;
                }
                else
                {
                    // Keep the prior document rather than writing an invalid one.
                    logger.LogWarning(
                        "Foundry profile updater document failed validation twice; keeping prior "
                        + "document. Errors: {Errors}",
                        string.Join(" | ", validation.Errors));
                    await transcripts.WriteAsync(
                        new CollaborationAgentTranscript
                        {
                            Agent = FoundryCollaborationAgents.ProfileUpdaterAgentName,
                            Source = "foundry",
                            Prompt = prompt,
                            RetrievedProfile = current,
                            TurnContext = context,
                            Events = events,
                            ResponseText = retryResponse.Text ?? agentResponse.Text,
                            ResponseObject = new
                            {
                                validation = "rejected",
                                errors = validation.Errors,
                            },
                        },
                        cancellationToken);
                    return new CollaborationProfileUpdateResult(
                        current,
                        null,
                        ValidationResult: "rejected",
                        RawRequest: prompt,
                        RawResponse: retryResponse.Text ?? agentResponse.Text);
                }
            }

            var updated = current with { Document = document!, Source = "llm" };
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
                        validation = validationResult,
                    },
                },
                cancellationToken);

            return new CollaborationProfileUpdateResult(
                updated,
                string.IsNullOrWhiteSpace(changeReason) ? null : changeReason,
                ValidationResult: validationResult,
                RawRequest: prompt,
                RawResponse: agentResponse.Text);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Foundry profile updater failed; falling back to stub updater.");
            return await FallbackAsync(
                current,
                events,
                context,
                prompt,
                "error",
                responseText: null,
                error: ex.ToString(),
                cancellationToken);
        }
    }

    private async Task<CollaborationProfileUpdateResult> FallbackAsync(
        BeliefProfileDto current,
        IReadOnlyList<InteractionDto> events,
        CollaborationProfileUpdateContext? context,
        string prompt,
        string source,
        string? responseText,
        string? error,
        CancellationToken cancellationToken)
    {
        var stub = fallback.UpdateFromObservations(current, events);
        var stubReason = StubCollaborationProfileUpdater.SummarizeChangeReason(events);
        await transcripts.WriteAsync(
            new CollaborationAgentTranscript
            {
                Agent = FoundryCollaborationAgents.ProfileUpdaterAgentName,
                Source = source,
                Prompt = prompt,
                RetrievedProfile = current,
                TurnContext = context,
                Events = events,
                ResponseText = responseText,
                ResponseObject = new { appliedProfile = stub, changeReason = stubReason },
                Error = error,
            },
            cancellationToken);
        return new CollaborationProfileUpdateResult(
            stub,
            stubReason,
            ValidationResult: "ok",
            RawRequest: prompt,
            RawResponse: responseText);
    }
}
