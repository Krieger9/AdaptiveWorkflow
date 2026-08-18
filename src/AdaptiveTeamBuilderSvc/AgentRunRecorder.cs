using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace AdaptiveTeamBuilderSvc;

/// <summary>Approval decision recorded for one adaptation (see IAdaptationApprovalPolicy).</summary>
public sealed class AdaptationApprovalRecord
{
    public string AdaptationId { get; set; } = string.Empty;

    /// <summary>preferred-layout | suggestion</summary>
    public string AdaptationKind { get; set; } = string.Empty;

    public bool Approved { get; set; }

    /// <summary>Policy that made the decision, e.g. auto-approve.</summary>
    public string Policy { get; set; } = string.Empty;

    /// <summary>Belief the adaptation draws on, when stated.</summary>
    public string? Belief { get; set; }

    public string? Rationale { get; set; }

    public DateTime DecidedAt { get; set; }
}

/// <summary>Per-dimension observation counter — instrumentation only, never fed to prompts or UI.</summary>
public sealed class ShadowCounterSnapshot
{
    public int For { get; set; }

    public int Against { get; set; }

    public string? FirstSeen { get; set; }
}

/// <summary>Token counts and provider metadata for one model response.</summary>
public sealed class AgentTokenUsageRecord
{
    public long? InputTokens { get; set; }

    public long? OutputTokens { get; set; }

    public long? TotalTokens { get; set; }

    public long? CachedInputTokens { get; set; }

    public long? ReasoningTokens { get; set; }

    /// <summary>
    /// Serialized SDK usage object. This preserves provider-specific counters that the common
    /// Microsoft.Extensions.AI properties do not expose directly.
    /// </summary>
    public string? RawUsage { get; set; }
}

/// <summary>
/// Complete diagnostic record for one physical model call. A logical profile update can contain
/// two of these when the first document fails validation and the updater retries.
/// </summary>
public sealed class AgentModelAttemptRecord
{
    public int Attempt { get; set; }

    /// <summary>initial | validation-retry</summary>
    public string Purpose { get; set; } = string.Empty;

    public string StartedAtUtc { get; set; } = string.Empty;

    public string? CompletedAtUtc { get; set; }

    public long LatencyMs { get; set; }

    public int RequestCharacters { get; set; }

    public int ResponseCharacters { get; set; }

    public int? RawParsedDocumentCharacters { get; set; }

    public int? ParsedDocumentCharacters { get; set; }

    public bool NormalizationChangedDocument { get; set; }

    public string RawRequest { get; set; } = string.Empty;

    public string? RawResponse { get; set; }

    public string? StructuredResult { get; set; }

    public string? ResponseId { get; set; }

    public string? AgentId { get; set; }

    public string? ProviderCreatedAtUtc { get; set; }

    public string? FinishReason { get; set; }

    public AgentTokenUsageRecord? Usage { get; set; }

    public string? RawProviderResponseType { get; set; }

    public string? RawProviderResponse { get; set; }

    public string? AdditionalProperties { get; set; }

    public IReadOnlyList<string> ValidationErrors { get; set; } = [];

    public string? Error { get; set; }
}

/// <summary>Diagnostics produced inside the profile updater itself.</summary>
public sealed class ProfileUpdateDiagnosticRecord
{
    public string? RunId { get; set; }

    public string Model { get; set; } = string.Empty;

    public string Endpoint { get; set; } = string.Empty;

    public string SystemInstructions { get; set; } = string.Empty;

    public string AgentFrameworkSdkVersion { get; set; } = string.Empty;

    public string OpenAiSdkVersion { get; set; } = string.Empty;

    public string SessionMode { get; set; } = "stateless (session: null)";

    public string RunOptions { get; set; } = "provider/model defaults (options: null)";

    public string TimingMode { get; set; } =
        "non-streaming; each attempt has total wall time but no time-to-first-token";

    public string StartedAtUtc { get; set; } = string.Empty;

    public string? CompletedAtUtc { get; set; }

    public long TotalUpdaterMs { get; set; }

    public long TranscriptWriteMs { get; set; }

    public int InputProfileCharacters { get; set; }

    public int InputEventCount { get; set; }

    public int UserEvidenceEventCount { get; set; }

    public int RecentDigestCount { get; set; }

    public int PromptCharacters { get; set; }

    public int OutputProfileCharacters { get; set; }

    public bool DocumentChanged { get; set; }

    public string? FinalValidationResult { get; set; }

    public IReadOnlyList<AgentModelAttemptRecord> Attempts { get; set; } = [];
}

/// <summary>Queue behavior for a profile update, including work discarded by coalescing.</summary>
public sealed class AgentRunQueueRecord
{
    public string? SelectedItemEnqueuedAtUtc { get; set; }

    public string? OldestItemEnqueuedAtUtc { get; set; }

    public string ProcessingStartedAtUtc { get; set; } = string.Empty;

    public long SelectedItemQueueWaitMs { get; set; }

    public long OldestItemQueueWaitMs { get; set; }

    public int SupersededWorkItemCount { get; set; }

    public IReadOnlyList<string> SupersededInteractionIds { get; set; } = [];
}

/// <summary>Wall-clock breakdown of the non-model and model phases of one profile update.</summary>
public sealed class AgentRunTimingRecord
{
    public long ProfileLoadMs { get; set; }

    public long ContextPreparationMs { get; set; }

    public long AgentUpdateMs { get; set; }

    public long ProfileSaveMs { get; set; }

    public long TurnDigestPersistenceMs { get; set; }

    public long RevisionPersistenceMs { get; set; }

    public long ShadowCounterPersistenceMs { get; set; }

    public long ProcessingTotalMs { get; set; }

    public long EndToEndFromSelectedEnqueueMs { get; set; }

    public long UnattributedMs { get; set; }
}

/// <summary>
/// One run record per agent invocation: data/runs/{runId}.json.
/// The observability harness is the primary deliverable — if the framework works and
/// the harness doesn't, we learn nothing.
/// </summary>
public sealed class AgentRunRecord
{
    public string RunId { get; set; } = string.Empty;

    public string Ts { get; set; } = string.Empty;

    public int Tier { get; set; } = 1;

    /// <summary>advisor | profile-updater</summary>
    public string Agent { get; set; } = string.Empty;

    /// <summary>advise | foundry | llm | error (historical records may contain stub labels)</summary>
    public string Source { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public string? SessionId { get; set; }

    /// <summary>flush-on-action | bootstrap | manual-replay</summary>
    public string Trigger { get; set; } = "flush-on-action";

    /// <summary>Hash of the system prompt / instructions.</summary>
    public string PromptVersion { get; set; } = string.Empty;

    public string? Model { get; set; }

    public string? RunOptions { get; set; }

    /// <summary>Hash of the assembled surface tree context.</summary>
    public string? ContextHash { get; set; }

    public string GlossaryVersion { get; set; } = string.Empty;

    public IReadOnlyList<string> InputInteractionIds { get; set; } = [];

    public int? ProfileVersionIn { get; set; }

    /// <summary>Full assembled prompt.</summary>
    public string RawRequest { get; set; } = string.Empty;

    /// <summary>Full agent output, pre-validation.</summary>
    public string? RawResponse { get; set; }

    /// <summary>ok | retried | rejected</summary>
    public string? ValidationResult { get; set; }

    public int? ProfileVersionOut { get; set; }

    /// <summary>Unified diff of the profile document.</summary>
    public string? ProfileDiff { get; set; }

    public IReadOnlyList<AdaptationApprovalRecord> Approvals { get; set; } = [];

    /// <summary>Shadow counters at the moment of this run (instrumentation only).</summary>
    public IReadOnlyDictionary<string, ShadowCounterSnapshot>? ShadowCounters { get; set; }

    /// <summary>Queue delay/coalescing details for background profile updates.</summary>
    public AgentRunQueueRecord? Queue { get; set; }

    /// <summary>Per-phase processing timings for background profile updates.</summary>
    public AgentRunTimingRecord? Timings { get; set; }

    /// <summary>Every physical LLM attempt and its provider/token/validation diagnostics.</summary>
    public ProfileUpdateDiagnosticRecord? ProfileUpdateDiagnostics { get; set; }

    public long LatencyMs { get; set; }

    public string? Error { get; set; }
}

public sealed record AgentRunSummary(
    string RunId,
    string Ts,
    int Tier,
    string Agent,
    string Source,
    string UserId,
    string? SessionId,
    string Trigger,
    string? ValidationResult,
    long LatencyMs);

public interface IAgentRunRecorder
{
    Task WriteAsync(AgentRunRecord record, CancellationToken cancellationToken);

    Task<IReadOnlyList<AgentRunSummary>> ListAsync(int take, CancellationToken cancellationToken);

    Task<AgentRunRecord?> GetAsync(string runId, CancellationToken cancellationToken);
}

public sealed class FileAgentRunRecorder(
    IOptions<AgentFrameworkOptions> options,
    IHostEnvironment environment,
    ILogger<FileAgentRunRecorder> logger) : IAgentRunRecorder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public static string NewRunId() =>
        $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}"[..40];

    /// <summary>Stable short hash for prompt/context versioning in run records.</summary>
    public static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexStringLower(bytes)[..16];
    }

    public async Task WriteAsync(AgentRunRecord record, CancellationToken cancellationToken)
    {
        try
        {
            var directory = RunsDirectory();
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, $"{FileInteractionLog.SanitizeToken(record.RunId)}.json");
            await File.WriteAllTextAsync(
                path,
                JsonSerializer.Serialize(record, JsonOptions),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to write agent run record {RunId}.", record.RunId);
        }
    }

    public async Task<IReadOnlyList<AgentRunSummary>> ListAsync(
        int take,
        CancellationToken cancellationToken)
    {
        var directory = RunsDirectory();
        if (!Directory.Exists(directory))
        {
            return [];
        }

        var summaries = new List<AgentRunSummary>();
        var files = Directory.EnumerateFiles(directory, "*.json")
            .OrderByDescending(f => f, StringComparer.Ordinal)
            .Take(Math.Clamp(take, 1, 500));

        foreach (var file in files)
        {
            var record = await ReadRecordAsync(file, cancellationToken);
            if (record is not null)
            {
                summaries.Add(new AgentRunSummary(
                    record.RunId,
                    record.Ts,
                    record.Tier,
                    record.Agent,
                    record.Source,
                    record.UserId,
                    record.SessionId,
                    record.Trigger,
                    record.ValidationResult,
                    record.LatencyMs));
            }
        }

        return summaries;
    }

    public async Task<AgentRunRecord?> GetAsync(string runId, CancellationToken cancellationToken)
    {
        var path = Path.Combine(
            RunsDirectory(),
            $"{FileInteractionLog.SanitizeToken(runId)}.json");
        return File.Exists(path) ? await ReadRecordAsync(path, cancellationToken) : null;
    }

    private async Task<AgentRunRecord?> ReadRecordAsync(
        string path,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<AgentRunRecord>(
                stream, JsonOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to read agent run record at {Path}.", path);
            return null;
        }
    }

    private string RunsDirectory() =>
        Path.Combine(PathUtilities.Resolve(environment, options.Value.DataDirectory), "runs");
}
