using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace AdaptiveTeamBuilderSvc;

/// <summary>
/// Loads the app-supplied glossary (dimensions defined by observable behavior) and
/// manages agent-proposed dimensions. Proposed dimensions are live immediately;
/// promotion into the main glossary is a human decision made later.
/// </summary>
public sealed class GlossaryProvider
{
    private readonly string _glossaryPath;
    private readonly string _proposedPath;
    private readonly ILogger<GlossaryProvider> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public GlossaryProvider(
        IOptions<AgentFrameworkOptions> options,
        IHostEnvironment environment,
        ILogger<GlossaryProvider> logger)
    {
        _logger = logger;
        var dataRoot = PathUtilities.Resolve(environment, options.Value.DataDirectory);
        _glossaryPath = Path.Combine(dataRoot, "glossary.json");
        _proposedPath = Path.Combine(dataRoot, "proposed-dimensions.json");

        GlossaryJson = File.Exists(_glossaryPath)
            ? File.ReadAllText(_glossaryPath)
            : """{ "version": "missing", "dimensions": [] }""";

        try
        {
            using var doc = JsonDocument.Parse(GlossaryJson);
            Version = doc.RootElement.TryGetProperty("version", out var version)
                ? version.GetString() ?? "unknown"
                : "unknown";
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "glossary.json is not valid JSON; using empty glossary.");
            GlossaryJson = """{ "version": "invalid", "dimensions": [] }""";
            Version = "invalid";
        }
    }

    /// <summary>Raw glossary JSON (already written in behavioral terms for prompt use).</summary>
    public string GlossaryJson { get; }

    public string Version { get; }

    public string ReadProposedDimensionsJson()
    {
        try
        {
            return File.Exists(_proposedPath)
                ? File.ReadAllText(_proposedPath)
                : """{ "dimensions": [] }""";
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to read proposed-dimensions.json.");
            return """{ "dimensions": [] }""";
        }
    }

    /// <summary>
    /// Appends agent-proposed dimensions. Each proposal must carry a
    /// disconfirming_behavior — it forces the agent to make the dimension falsifiable
    /// at the moment it invents it.
    /// </summary>
    public async Task AppendProposedDimensionsAsync(
        IReadOnlyList<ProposedDimension> proposals,
        CancellationToken cancellationToken)
    {
        if (proposals.Count == 0)
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var root = JsonNode.Parse(ReadProposedDimensionsJson()) as JsonObject
                ?? new JsonObject();
            if (root["dimensions"] is not JsonArray dimensions)
            {
                dimensions = [];
                root["dimensions"] = dimensions;
            }

            foreach (var proposal in proposals)
            {
                if (string.IsNullOrWhiteSpace(proposal.Id)
                    || string.IsNullOrWhiteSpace(proposal.DisconfirmingBehavior))
                {
                    _logger.LogWarning(
                        "Skipping proposed dimension '{Id}' without a disconfirming_behavior.",
                        proposal.Id);
                    continue;
                }

                var exists = dimensions.Any(node =>
                    string.Equals(
                        node?["id"]?.GetValue<string>(),
                        proposal.Id,
                        StringComparison.OrdinalIgnoreCase));
                if (exists)
                {
                    continue;
                }

                dimensions.Add(new JsonObject
                {
                    ["id"] = proposal.Id,
                    ["description"] = proposal.Description,
                    ["values"] = proposal.Values,
                    ["confirming_behavior"] = proposal.ConfirmingBehavior,
                    ["disconfirming_behavior"] = proposal.DisconfirmingBehavior,
                    ["origin"] = "proposed",
                    ["proposed_by"] = proposal.ProposedBy,
                    ["proposed_on"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                });
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_proposedPath)!);
            await File.WriteAllTextAsync(
                _proposedPath,
                root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }),
                cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }
}

/// <summary>An agent-invented preference dimension.</summary>
public sealed class ProposedDimension
{
    public string Id { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Enumerated behavioral value map, or "open" with a note on the prose form.</summary>
    public string Values { get; set; } = "open";

    public string ConfirmingBehavior { get; set; } = string.Empty;

    /// <summary>Required: what behavior would make this dimension less likely.</summary>
    public string DisconfirmingBehavior { get; set; } = string.Empty;

    /// <summary>Tier that proposed it, e.g. "tier-1".</summary>
    public string ProposedBy { get; set; } = "tier-1";
}

internal static class PathUtilities
{
    public static string Resolve(IHostEnvironment environment, string configured) =>
        Path.IsPathRooted(configured)
            ? configured
            : Path.GetFullPath(Path.Combine(environment.ContentRootPath, configured));
}
