using System.Text.Json;
using System.Text.Json.Serialization;
using AdaptiveTeamBuilder.Data.Contracts;
using AdaptiveTeamBuilder.Data.Entities;
using Microsoft.Extensions.Options;

namespace AdaptiveTeamBuilderSvc;

/// <summary>
/// One scripted interaction inside a persona turn. Contract-targeting actions
/// reference contracts by index into the demo-ordered contract list so persona
/// scripts stay valid across database rebuilds.
/// </summary>
public sealed record PersonaInteraction(
    string Action,
    int? ContractIndex = null,
    IReadOnlyDictionary<string, string>? Meta = null);

/// <summary>One decision turn: the interactions flushed together, as the UI would.</summary>
public sealed record PersonaTurn(IReadOnlyList<PersonaInteraction> Interactions);

public sealed record PersonaScript(
    string Name,
    string Description,
    IReadOnlyList<PersonaTurn> Turns);

/// <summary>
/// Phase 7 (stretch): synthetic personas. Loads scripted persona interaction streams
/// from <c>data/personas/*.json</c> and synthesizes real interactions against the
/// current contract list, so a believable belief profile can be pre-warmed through
/// the exact same observations pipeline before a stakeholder demo.
/// </summary>
public sealed class SyntheticPersonaProvider(
    IOptions<AgentFrameworkOptions> options,
    IHostEnvironment environment,
    ILogger<SyntheticPersonaProvider> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private string PersonasDirectory => Path.Combine(
        PathUtilities.Resolve(environment, options.Value.DataDirectory),
        "personas");

    public IReadOnlyList<string> ListNames()
    {
        if (!Directory.Exists(PersonasDirectory))
        {
            return [];
        }

        return Directory.EnumerateFiles(PersonasDirectory, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
    }

    public PersonaScript? Get(string name)
    {
        // Persona names come from the URL; restrict to the file's own basename set.
        if (!ListNames().Contains(name, StringComparer.Ordinal))
        {
            return null;
        }

        var path = Path.Combine(PersonasDirectory, name + ".json");
        try
        {
            return JsonSerializer.Deserialize<PersonaScript>(File.ReadAllText(path), JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Persona script {Name} is not valid JSON.", name);
            return null;
        }
    }

    /// <summary>
    /// Synthesizes the persona's scripted turns into concrete interaction batches
    /// against the given contracts, in session <paramref name="sessionId"/>.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<InteractionDto>> Synthesize(
        PersonaScript script,
        IReadOnlyList<Contract> contracts,
        string sessionId)
    {
        var turns = new List<IReadOnlyList<InteractionDto>>();
        var seq = 0;
        // Backdate so the stream reads as a natural session leading up to now.
        var interactionCount = script.Turns.Sum(t => t.Interactions.Count);
        var at = DateTime.UtcNow.AddSeconds(-2d * interactionCount);
        var choiceSet = contracts
            .Select(c => new ChoiceSetItemDto(c.Id.ToString("D"), ContractAttrs(c)))
            .ToList();

        foreach (var turn in script.Turns)
        {
            var batch = new List<InteractionDto>();
            foreach (var scripted in turn.Interactions)
            {
                seq++;
                at = at.AddSeconds(2);
                Contract? contract = null;
                if (scripted.ContractIndex is int index && index >= 0 && index < contracts.Count)
                {
                    contract = contracts[index];
                }

                batch.Add(new InteractionDto(
                    Id: $"i_{seq}_{Guid.NewGuid().ToString("N")[..6]}",
                    At: at,
                    SessionId: sessionId,
                    Seq: seq,
                    SurfacePath: ["page:contracts", "section:contracts.list"],
                    Action: scripted.Action,
                    ControlId: contract?.Id.ToString("D"),
                    Label: contract is null ? LabelFor(scripted) : $"{contract.Code} {contract.Title}",
                    ValueBefore: null,
                    ValueAfter: null,
                    Causation: "user",
                    Reversal: null,
                    Entity: contract is null
                        ? null
                        : new EntityRefDto("contract", contract.Id.ToString("D"), ContractAttrs(contract)),
                    ChoiceSet: scripted.Action is "control.expand" or "control.select" ? choiceSet : null,
                    Meta: scripted.Meta));
            }

            turns.Add(batch);
        }

        return turns;
    }

    private static string? LabelFor(PersonaInteraction scripted) =>
        scripted.Action == "view.change" ? "signals-display" : null;

    private static Dictionary<string, string> ContractAttrs(Contract contract) => new()
    {
        ["code"] = contract.Code,
        ["title"] = contract.Title,
        ["estimatedContractValue"] = contract.EstimatedContractValue.ToString("0.##"),
        ["estimatedProfit"] = contract.EstimatedProfit.ToString("0.##"),
        ["estimatedMarginPercent"] = contract.EstimatedMarginPercent.ToString("0.##"),
        ["winProbabilityPercent"] = contract.WinProbabilityPercent.ToString("0.##"),
    };
}
