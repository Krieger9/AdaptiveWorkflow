namespace AdaptiveTeamBuilderSvc;

public sealed class AgentFrameworkOptions
{
    public const string SectionName = "AgentFramework";

    /// <summary>Foundry project endpoint (documented for later persistent-agent use).</summary>
    public string ProjectEndpoint { get; set; } =
        "https://adaptiveworkflowfoundry.services.ai.azure.com/api/projects/AdaptiveWorkFlowAgents";

    /// <summary>OpenAI Responses v1 endpoint on Foundry.</summary>
    public string OpenAIEndpoint { get; set; } =
        "https://AdaptiveWorkflowFoundry.services.ai.azure.com/openai/v1";

    public string DeploymentName { get; set; } = "gpt-5.4-mini";

    /// <summary>Set via User Secrets: AgentFramework:ApiKey</summary>
    public string? ApiKey { get; set; }

    /// <summary>When true, write each agent prompt/response to disk for inspection.</summary>
    public bool LogTranscripts { get; set; } = true;

    /// <summary>
    /// Directory for transcript files, relative to the content root unless absolute.
    /// </summary>
    public string TranscriptDirectory { get; set; } = "logs/collaboration";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApiKey)
        && !string.IsNullOrWhiteSpace(OpenAIEndpoint)
        && !string.IsNullOrWhiteSpace(DeploymentName);
}
