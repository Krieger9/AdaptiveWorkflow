using System.Text;
using System.Text.Json;
using AdaptiveTeamBuilder.Data.Contracts;
using Microsoft.Extensions.Options;

namespace AdaptiveTeamBuilderSvc;

public sealed class CollaborationAgentTranscript
{
    /// <summary>Correlation id shared with data/runs/{runId}.json.</summary>
    public string? RunId { get; init; }

    public required string Agent { get; init; }

    /// <summary>foundry | error</summary>
    public required string Source { get; init; }

    public required string Prompt { get; init; }

    /// <summary>Profile retrieved from store before this agent turn.</summary>
    public BeliefProfileDto? RetrievedProfile { get; init; }

    /// <summary>Surface/view context for the turn, when available.</summary>
    public CollaborationProfileUpdateContext? TurnContext { get; init; }

    /// <summary>Semantic events for the turn, when available.</summary>
    public IReadOnlyList<InteractionDto>? Events { get; init; }

    public string? ResponseText { get; init; }

    public object? ResponseObject { get; init; }

    /// <summary>Per-attempt timing, usage, provider metadata, and validation details.</summary>
    public ProfileUpdateDiagnosticRecord? ProfileUpdateDiagnostics { get; init; }

    public string? Error { get; init; }
}

public interface ICollaborationAgentTranscriptLogger
{
    Task WriteAsync(CollaborationAgentTranscript transcript, CancellationToken cancellationToken = default);
}

/// <summary>
/// Writes one markdown file per agent turn under logs/collaboration for prompt/response inspection.
/// </summary>
public sealed class FileCollaborationAgentTranscriptLogger(
    IOptions<AgentFrameworkOptions> options,
    IHostEnvironment environment,
    ILogger<FileCollaborationAgentTranscriptLogger> logger) : ICollaborationAgentTranscriptLogger
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task WriteAsync(
        CollaborationAgentTranscript transcript,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.LogTranscripts)
        {
            return;
        }

        try
        {
            var directory = ResolveDirectory(settings.TranscriptDirectory);
            Directory.CreateDirectory(directory);

            var stamp = DateTime.UtcNow;
            var safeAgent = SanitizeFileToken(transcript.Agent);
            var runToken = string.IsNullOrWhiteSpace(transcript.RunId)
                ? string.Empty
                : $"-{SanitizeFileToken(transcript.RunId)}";
            var fileName =
                $"{stamp:yyyyMMdd-HHmmss-fff}-{safeAgent}-{SanitizeFileToken(transcript.Source)}{runToken}.md";
            var path = Path.Combine(directory, fileName);

            var body = BuildMarkdown(transcript, stamp, path);

            await _gate.WaitAsync(cancellationToken);
            try
            {
                await File.WriteAllTextAsync(path, body, Encoding.UTF8, cancellationToken);
            }
            finally
            {
                _gate.Release();
            }

            logger.LogDebug("Wrote collaboration agent transcript to {Path}", path);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to write collaboration agent transcript.");
        }
    }

    private string ResolveDirectory(string configured)
    {
        if (Path.IsPathRooted(configured))
        {
            return configured;
        }

        return Path.GetFullPath(Path.Combine(environment.ContentRootPath, configured));
    }

    private static string BuildMarkdown(
        CollaborationAgentTranscript transcript,
        DateTime stamp,
        string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {transcript.Agent}");
        sb.AppendLine();
        sb.AppendLine($"- utc: `{stamp:O}`");
        sb.AppendLine($"- source: `{transcript.Source}`");
        if (!string.IsNullOrWhiteSpace(transcript.RunId))
        {
            sb.AppendLine($"- run-id: `{transcript.RunId}`");
        }
        sb.AppendLine($"- file: `{path}`");
        sb.AppendLine();

        if (transcript.ProfileUpdateDiagnostics is not null)
        {
            sb.AppendLine("## Run diagnostics");
            sb.AppendLine();
            sb.AppendLine("```json");
            sb.AppendLine(JsonSerializer.Serialize(transcript.ProfileUpdateDiagnostics, JsonOptions));
            sb.AppendLine("```");
            sb.AppendLine();
        }

        if (transcript.RetrievedProfile is not null)
        {
            sb.AppendLine("## Retrieved user profile");
            sb.AppendLine();
            sb.AppendLine("```text");
            sb.AppendLine(
                CollaborationContextFormatter.FormatRetrievedProfile(transcript.RetrievedProfile));
            sb.AppendLine("```");
            sb.AppendLine();
        }

        if (transcript.TurnContext is not null || transcript.Events is { Count: > 0 })
        {
            sb.AppendLine("## Turn context");
            sb.AppendLine();
            sb.AppendLine("```text");
            if (transcript.TurnContext is { } ctx)
            {
                if (!string.IsNullOrWhiteSpace(ctx.SurfacePath))
                {
                    sb.AppendLine($"Surface: {ctx.SurfaceTitle ?? ctx.SurfacePath} ({ctx.SurfacePath})");
                }

                if (ctx.ViewState is not null)
                {
                    sb.AppendLine(
                        CollaborationContextFormatter.FormatViewState(
                            ctx.ViewState,
                            ctx.VisibleControlCount));
                    if (transcript.Events is { } patternEvents)
                    {
                        sb.AppendLine(
                            CollaborationContextFormatter.FormatComparisonPattern(
                                ctx.ViewState,
                                ctx.VisibleControlCount,
                                patternEvents));
                        sb.AppendLine(
                            CollaborationContextFormatter.FormatSignalRankComparison(
                                ctx.Controls,
                                ctx.ViewState,
                                patternEvents));
                    }
                }
            }

            if (transcript.Events is { Count: > 0 } events)
            {
                sb.AppendLine(
                    CollaborationContextFormatter.FormatSemanticActions(events));
                sb.AppendLine(
                    CollaborationContextFormatter.FormatActionTiming(events));
            }

            sb.AppendLine("```");
            sb.AppendLine();
        }

        sb.AppendLine("## Prompt");
        sb.AppendLine();
        sb.AppendLine("```text");
        sb.AppendLine(transcript.Prompt.TrimEnd());
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## Response");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(transcript.Error))
        {
            sb.AppendLine("```text");
            sb.AppendLine(transcript.Error.TrimEnd());
            sb.AppendLine("```");
            sb.AppendLine();
        }

        if (transcript.ResponseObject is not null)
        {
            sb.AppendLine("```json");
            sb.AppendLine(JsonSerializer.Serialize(transcript.ResponseObject, JsonOptions));
            sb.AppendLine("```");
        }
        else if (!string.IsNullOrWhiteSpace(transcript.ResponseText))
        {
            sb.AppendLine("```text");
            sb.AppendLine(transcript.ResponseText.TrimEnd());
            sb.AppendLine("```");
        }
        else if (string.IsNullOrWhiteSpace(transcript.Error))
        {
            sb.AppendLine("_(empty)_");
        }

        return sb.ToString();
    }

    private static string SanitizeFileToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        var chars = value.Trim().Select(ch =>
            char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_' ? ch : '-').ToArray();
        return new string(chars).Trim('-').ToLowerInvariant();
    }
}
