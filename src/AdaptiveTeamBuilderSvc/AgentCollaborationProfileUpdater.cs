using System.Text.Json;
using System.Diagnostics;
using AdaptiveTeamBuilder.Data.Contracts;
using Microsoft.Agents.AI;
namespace AdaptiveTeamBuilderSvc;

/// <summary>
/// Foundry-backed profile updater. Validates the returned belief document, retries once with
/// the validation errors appended, and keeps the prior document when Foundry cannot provide a
/// valid update.
/// </summary>
public sealed class AgentCollaborationProfileUpdater(
    FoundryCollaborationAgents agents,
    ICollaborationAgentTranscriptLogger transcripts,
    Microsoft.Extensions.Options.IOptions<AgentFrameworkOptions> options,
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
        var updaterStopwatch = Stopwatch.StartNew();
        var attempts = new List<AgentModelAttemptRecord>();
        var diagnostics = new ProfileUpdateDiagnosticRecord
        {
            RunId = context?.RunId,
            Model = options.Value.DeploymentName,
            Endpoint = options.Value.OpenAIEndpoint,
            SystemInstructions = FoundryCollaborationAgents.ProfileUpdaterInstructions,
            RunOptions =
                $"reasoning.effort={agents.ConfiguredReasoningEffort}; reasoning.output=none",
            AgentFrameworkSdkVersion =
                typeof(AIAgent).Assembly.GetName().Version?.ToString() ?? "unknown",
            OpenAiSdkVersion =
                typeof(OpenAI.OpenAIClientOptions).Assembly.GetName().Version?.ToString()
                ?? "unknown",
            StartedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
            InputProfileCharacters = current.Document.Length,
            InputEventCount = events.Count,
            UserEvidenceEventCount = StubCollaborationProfileUpdater.UserEvidence(events).Count,
            RecentDigestCount = context?.RecentTurnDigests?.Count ?? 0,
            PromptCharacters = prompt.Length,
            Attempts = attempts,
        };
        AgentModelAttemptRecord? activeAttempt = null;
        Stopwatch? activeAttemptStopwatch = null;

        try
        {
            activeAttempt = StartAttempt(1, "initial", prompt);
            attempts.Add(activeAttempt);
            activeAttemptStopwatch = Stopwatch.StartNew();
            var agentResponse = await agents.ProfileUpdater.RunAsync<ProfileUpdateAgentResult>(
                prompt,
                session: null,
                serializerOptions: SerializerOptions,
                options: agents.CreateRunOptions(),
                cancellationToken: cancellationToken);

            var rawDocument = agentResponse.Result?.ProfileDocument?.Trim();
            var document = NormalizeReturnedDocument(rawDocument);
            CompleteAttempt(
                activeAttempt,
                activeAttemptStopwatch,
                agentResponse,
                agentResponse.Text,
                rawDocument,
                document);
            activeAttempt.StructuredResult = SerializeDiagnosticObject(agentResponse.Result);
            activeAttempt = null;
            activeAttemptStopwatch = null;
            if (string.IsNullOrWhiteSpace(document))
            {
                logger.LogWarning(
                    "Foundry profile updater returned an empty document; keeping the prior profile.");
                return await KeepCurrentAsync(
                    current,
                    events,
                    context,
                    prompt,
                    "foundry",
                    agentResponse.Text,
                    validationResult: "rejected",
                    reason: "Foundry returned an empty profile document.",
                    error: null,
                    diagnostics: diagnostics,
                    updaterStopwatch: updaterStopwatch,
                    cancellationToken: cancellationToken);
            }

            var validationResult = "ok";
            var validation = BeliefDocumentFormat.Validate(document);
            if (!validation.IsValid)
            {
                attempts[^1].ValidationErrors = validation.Errors;
                // Retry once with the validation errors appended to the prompt.
                var retryPrompt =
                    prompt
                    + "\n\nYour previous document failed validation. Fix these issues and return "
                    + "the COMPLETE corrected document:\n- "
                    + string.Join("\n- ", validation.Errors);
                activeAttempt = StartAttempt(2, "validation-retry", retryPrompt);
                attempts.Add(activeAttempt);
                activeAttemptStopwatch = Stopwatch.StartNew();
                var retryResponse = await agents.ProfileUpdater.RunAsync<ProfileUpdateAgentResult>(
                    retryPrompt,
                    session: null,
                    serializerOptions: SerializerOptions,
                    options: agents.CreateRunOptions(),
                    cancellationToken: cancellationToken);

                var rawRetryDocument = retryResponse.Result?.ProfileDocument?.Trim();
                var retryDocument = NormalizeReturnedDocument(rawRetryDocument);
                CompleteAttempt(
                    activeAttempt,
                    activeAttemptStopwatch,
                    retryResponse,
                    retryResponse.Text,
                    rawRetryDocument,
                    retryDocument);
                activeAttempt.StructuredResult = SerializeDiagnosticObject(retryResponse.Result);
                activeAttempt = null;
                activeAttemptStopwatch = null;
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
                    attempts[^1].ValidationErrors = retryValidation?.Errors
                        ?? ["Document is empty after normalization."];
                    // Keep the prior document rather than writing an invalid one.
                    logger.LogWarning(
                        "Foundry profile updater document failed validation twice; keeping prior "
                        + "document. Errors: {Errors}",
                        string.Join(" | ", validation.Errors));
                    SnapshotDiagnostics(
                        diagnostics,
                        updaterStopwatch,
                        current.Document,
                        "rejected");
                    var transcriptStopwatch = Stopwatch.StartNew();
                    await transcripts.WriteAsync(
                        new CollaborationAgentTranscript
                        {
                            RunId = context?.RunId,
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
                            ProfileUpdateDiagnostics = diagnostics,
                        },
                        cancellationToken);
                    transcriptStopwatch.Stop();
                    diagnostics.TranscriptWriteMs += transcriptStopwatch.ElapsedMilliseconds;
                    CompleteDiagnostics(
                        diagnostics,
                        updaterStopwatch,
                        current.Document,
                        "rejected");
                    return new CollaborationProfileUpdateResult(
                        current,
                        null,
                        ValidationResult: "rejected",
                        RawRequest: prompt,
                        RawResponse: retryResponse.Text ?? agentResponse.Text,
                        Diagnostics: diagnostics);
                }
            }

            var updated = current with { Document = document!, Source = "llm" };
            var changeReason = agentResponse.Result?.ChangeReason?.Trim();

            SnapshotDiagnostics(
                diagnostics,
                updaterStopwatch,
                updated.Document,
                validationResult,
                !string.Equals(updated.Document, current.Document, StringComparison.Ordinal));
            var successTranscriptStopwatch = Stopwatch.StartNew();
            await transcripts.WriteAsync(
                new CollaborationAgentTranscript
                {
                    RunId = context?.RunId,
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
                    ProfileUpdateDiagnostics = diagnostics,
                },
                cancellationToken);
            successTranscriptStopwatch.Stop();
            diagnostics.TranscriptWriteMs += successTranscriptStopwatch.ElapsedMilliseconds;
            CompleteDiagnostics(
                diagnostics,
                updaterStopwatch,
                updated.Document,
                validationResult,
                !string.Equals(updated.Document, current.Document, StringComparison.Ordinal));

            return new CollaborationProfileUpdateResult(
                updated,
                string.IsNullOrWhiteSpace(changeReason) ? null : changeReason,
                ValidationResult: validationResult,
                RawRequest: prompt,
                RawResponse: agentResponse.Text,
                Diagnostics: diagnostics);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (activeAttempt is not null)
            {
                activeAttemptStopwatch?.Stop();
                activeAttempt.LatencyMs = activeAttemptStopwatch?.ElapsedMilliseconds ?? 0;
                activeAttempt.CompletedAtUtc = DateTimeOffset.UtcNow.ToString("O");
                activeAttempt.Error = ex.ToString();
            }
            logger.LogError(ex, "Foundry profile updater failed; keeping the prior profile.");
            return await KeepCurrentAsync(
                current,
                events,
                context,
                prompt,
                "error",
                responseText: null,
                validationResult: "error",
                reason: "Foundry profile update failed; no profile change was applied.",
                error: ex.ToString(),
                diagnostics: diagnostics,
                updaterStopwatch: updaterStopwatch,
                cancellationToken: cancellationToken);
        }
    }

    private async Task<CollaborationProfileUpdateResult> KeepCurrentAsync(
        BeliefProfileDto current,
        IReadOnlyList<InteractionDto> events,
        CollaborationProfileUpdateContext? context,
        string prompt,
        string source,
        string? responseText,
        string validationResult,
        string reason,
        string? error,
        ProfileUpdateDiagnosticRecord diagnostics,
        Stopwatch updaterStopwatch,
        CancellationToken cancellationToken)
    {
        SnapshotDiagnostics(
            diagnostics,
            updaterStopwatch,
            current.Document,
            validationResult);
        var transcriptStopwatch = Stopwatch.StartNew();
        await transcripts.WriteAsync(
            new CollaborationAgentTranscript
            {
                RunId = context?.RunId,
                Agent = FoundryCollaborationAgents.ProfileUpdaterAgentName,
                Source = source,
                Prompt = prompt,
                RetrievedProfile = current,
                TurnContext = context,
                Events = events,
                ResponseText = responseText,
                ResponseObject = new
                {
                    appliedProfile = current,
                    validation = validationResult,
                    reason,
                },
                ProfileUpdateDiagnostics = diagnostics,
                Error = error,
            },
            cancellationToken);
        transcriptStopwatch.Stop();
        diagnostics.TranscriptWriteMs += transcriptStopwatch.ElapsedMilliseconds;
        CompleteDiagnostics(
            diagnostics,
            updaterStopwatch,
            current.Document,
            validationResult);
        return new CollaborationProfileUpdateResult(
            current,
            ChangeReason: null,
            ValidationResult: validationResult,
            RawRequest: prompt,
            RawResponse: responseText,
            Diagnostics: diagnostics);
    }

    private static AgentModelAttemptRecord StartAttempt(
        int attempt,
        string purpose,
        string prompt) =>
        new()
        {
            Attempt = attempt,
            Purpose = purpose,
            StartedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
            RequestCharacters = prompt.Length,
            RawRequest = prompt,
        };

    private static void CompleteAttempt(
        AgentModelAttemptRecord attempt,
        Stopwatch stopwatch,
        AgentResponse response,
        string? rawResponse,
        string? rawParsedDocument,
        string? parsedDocument)
    {
        stopwatch.Stop();
        attempt.CompletedAtUtc = DateTimeOffset.UtcNow.ToString("O");
        attempt.LatencyMs = stopwatch.ElapsedMilliseconds;
        attempt.RawResponse = rawResponse;
        attempt.ResponseCharacters = rawResponse?.Length ?? 0;
        attempt.RawParsedDocumentCharacters = rawParsedDocument?.Length;
        attempt.ParsedDocumentCharacters = parsedDocument?.Length;
        attempt.NormalizationChangedDocument = !string.Equals(
            rawParsedDocument,
            parsedDocument,
            StringComparison.Ordinal);
        attempt.ResponseId = response.ResponseId;
        attempt.AgentId = response.AgentId;
        attempt.ProviderCreatedAtUtc = response.CreatedAt?.ToString("O");
        attempt.FinishReason = response.FinishReason?.ToString();
        if (response.Usage is { } usage)
        {
            attempt.Usage = new AgentTokenUsageRecord
            {
                InputTokens = usage.InputTokenCount,
                OutputTokens = usage.OutputTokenCount,
                TotalTokens = usage.TotalTokenCount,
                CachedInputTokens = usage.CachedInputTokenCount,
                ReasoningTokens = usage.ReasoningTokenCount,
                RawUsage = SerializeDiagnosticObject(usage),
            };
        }

        attempt.RawProviderResponseType = response.RawRepresentation?.GetType().FullName;
        attempt.RawProviderResponse = SerializeDiagnosticObject(response.RawRepresentation);
        attempt.AdditionalProperties = SerializeDiagnosticObject(response.AdditionalProperties);
    }

    private static void CompleteDiagnostics(
        ProfileUpdateDiagnosticRecord diagnostics,
        Stopwatch updaterStopwatch,
        string outputDocument,
        string validationResult,
        bool documentChanged = false)
    {
        updaterStopwatch.Stop();
        SnapshotDiagnostics(
            diagnostics,
            updaterStopwatch,
            outputDocument,
            validationResult,
            documentChanged);
    }

    private static void SnapshotDiagnostics(
        ProfileUpdateDiagnosticRecord diagnostics,
        Stopwatch updaterStopwatch,
        string outputDocument,
        string validationResult,
        bool documentChanged = false)
    {
        diagnostics.CompletedAtUtc = DateTimeOffset.UtcNow.ToString("O");
        diagnostics.TotalUpdaterMs = updaterStopwatch.ElapsedMilliseconds;
        diagnostics.OutputProfileCharacters = outputDocument.Length;
        diagnostics.DocumentChanged = documentChanged;
        diagnostics.FinalValidationResult = validationResult;
    }

    private static string? SerializeDiagnosticObject(object? value)
    {
        if (value is null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Serialize(value, value.GetType(), SerializerOptions);
        }
        catch
        {
            return value.ToString();
        }
    }

    /// <summary>
    /// The model sometimes echoes the retrieved-profile envelope (tier/version/source/updatedAt/
    /// document:) it was shown in the prompt, with the belief markdown indented under 'document:'.
    /// Strip that wrapper and de-indent so the raw belief markdown reaches validation.
    /// </summary>
    internal static string? NormalizeReturnedDocument(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return raw;
        }

        var text = raw.Replace("\r\n", "\n");
        var marker = text.IndexOf("document:", StringComparison.OrdinalIgnoreCase);
        if (marker < 0)
        {
            return raw.Trim();
        }

        // Only treat it as an envelope when 'document:' is a leading key (start or line start).
        var isLeadingKey = marker == 0 || text[marker - 1] == '\n';
        if (!isLeadingKey)
        {
            return raw.Trim();
        }

        var afterMarker = text[(marker + "document:".Length)..].TrimStart('\n');
        if (afterMarker.Length == 0)
        {
            return raw.Trim();
        }

        var lines = afterMarker.Split('\n');
        var deindented = lines.Select(l => l.StartsWith("  ", StringComparison.Ordinal) ? l[2..] : l);
        return string.Join("\n", deindented).Trim();
    }
}
