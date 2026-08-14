using System.Security.Claims;
using System.Text.Json;
using AdaptiveTeamBuilder.Data;
using AdaptiveTeamBuilder.Data.Contracts;
using AdaptiveTeamBuilder.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdaptiveTeamBuilderSvc;

public static class CollaborationEndpoints
{
    private const string ObjectIdClaim =
        "http://schemas.microsoft.com/identity/claims/objectidentifier";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public static RouteGroupBuilder MapCollaborationEndpoints(
        this IEndpointRouteBuilder app,
        bool includeDevEndpoints = false)
    {
        var group = app.MapGroup("/api/collaboration")
            .WithTags("Collaboration")
            .RequireAuthorization("AccessAsUser");

        group.MapGet("/profile", GetProfileAsync);
        group.MapPost("/advise", AdviseAsync);
        group.MapPost("/observations", SubmitObservationsAsync);

        if (includeDevEndpoints)
        {
            // Dev-only observability harness: run records + interaction replay.
            group.MapGet("/runs", ListRunsAsync);
            group.MapGet("/runs/{runId}", GetRunAsync);
            group.MapGet("/sessions", ListSessions);
            group.MapGet("/sessions/{sessionId}/interactions", GetSessionInteractionsAsync);
            group.MapPost("/replay", ReplayAsync);
            group.MapGet("/personas", ListPersonas);
            group.MapPost("/personas/{name}/run", RunPersonaAsync);
        }

        return group;
    }

    private static async Task<IResult> GetProfileAsync(
        ClaimsPrincipal principal,
        AdaptiveTeamBuilderDbContext db,
        IBeliefProfileStore profileStore,
        CancellationToken cancellationToken)
    {
        var user = await ResolveUserAsync(principal, db, cancellationToken);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var profile = await profileStore.GetAsync(user.Id, cancellationToken);
        return Results.Ok(new ProfileResponse(profile));
    }

    private static async Task<IResult> AdviseAsync(
        AdviseRequest request,
        ClaimsPrincipal principal,
        AdaptiveTeamBuilderDbContext db,
        IBeliefProfileStore profileStore,
        ICollaborationAdvisor advisor,
        IAdaptationApprovalPolicy approvalPolicy,
        IAgentRunRecorder runRecorder,
        GlossaryProvider glossary,
        CancellationToken cancellationToken)
    {
        var user = await ResolveUserAsync(principal, db, cancellationToken);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var profile = await profileStore.GetAsync(user.Id, cancellationToken);
        var response = await advisor.AdviseAsync(request, profile, cancellationToken);

        // Approval seam: nothing reaches the UI without a logged decision.
        var approvals = await approvalPolicy.DecideAsync(
            user.Id,
            response.PreferredLayout,
            response.Suggestions,
            cancellationToken);
        response = FilterApproved(response, approvals);
        stopwatch.Stop();

        await runRecorder.WriteAsync(
            new AgentRunRecord
            {
                RunId = FileAgentRunRecorder.NewRunId(),
                Ts = DateTime.UtcNow.ToString("O"),
                Tier = 1,
                Agent = FoundryCollaborationAgents.AdvisorAgentName,
                Source = "advise",
                UserId = user.Id.ToString("D"),
                Trigger = "bootstrap",
                PromptVersion = FileAgentRunRecorder.Hash(
                    FoundryCollaborationAgents.AdvisorInstructions),
                ContextHash = request.Surface.ContextHash,
                GlossaryVersion = glossary.Version,
                InputInteractionIds = InteractionIds(request.Interactions),
                ProfileVersionIn = profile.Version,
                ProfileVersionOut = profile.Version,
                RawRequest = response.PromptPreview,
                RawResponse = JsonSerializer.Serialize(
                    new { response.Suggestions, response.PreferredLayout },
                    JsonOptions),
                Approvals = approvals,
                LatencyMs = stopwatch.ElapsedMilliseconds,
            },
            cancellationToken);

        return Results.Ok(response);
    }

    private static async Task<IResult> SubmitObservationsAsync(
        ObservationsRequest request,
        ClaimsPrincipal principal,
        AdaptiveTeamBuilderDbContext db,
        IBeliefProfileStore profileStore,
        ICollaborationAdvisor advisor,
        ICollaborationProfileUpdateQueue updateQueue,
        IInteractionLog interactionLog,
        IAdaptationApprovalPolicy approvalPolicy,
        IAgentRunRecorder runRecorder,
        GlossaryProvider glossary,
        CancellationToken cancellationToken)
    {
        var user = await ResolveUserAsync(principal, db, cancellationToken);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        if (request.UserId != user.Id)
        {
            return Results.Json(
                new { error = "UserId does not match the authenticated user." },
                statusCode: StatusCodes.Status403Forbidden);
        }

        // Reversal computation happens server-side so every consumer sees the same flags.
        var interactions = CollaborationContextFormatter.FlagReversals(request.Interactions);

        // Stamp selected contract before advise/profile so the next list call rotates.
        await StampSelectedContractsAsync(db, interactions, cancellationToken);

        // Evidence persists twice: append-only JSONL for replay, DB rows for queries.
        await interactionLog.AppendAsync(
            user.Id,
            request.SessionId,
            interactions,
            cancellationToken);
        await PersistInteractionsAsync(db, user.Id, request.SessionId, interactions, cancellationToken);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var profile = await profileStore.GetAsync(user.Id, cancellationToken);
        var adviseRequest = new AdviseRequest(
            request.App,
            request.Surface,
            request.Controls,
            interactions);
        var advice = await advisor.AdviseAsync(adviseRequest, profile, cancellationToken);

        var approvals = await approvalPolicy.DecideAsync(
            user.Id,
            advice.PreferredLayout,
            advice.Suggestions,
            cancellationToken);
        advice = FilterApproved(advice, approvals);
        stopwatch.Stop();

        await runRecorder.WriteAsync(
            new AgentRunRecord
            {
                RunId = FileAgentRunRecorder.NewRunId(),
                Ts = DateTime.UtcNow.ToString("O"),
                Tier = 1,
                Agent = FoundryCollaborationAgents.AdvisorAgentName,
                Source = "advise",
                UserId = user.Id.ToString("D"),
                SessionId = request.SessionId,
                Trigger = "flush-on-action",
                PromptVersion = FileAgentRunRecorder.Hash(
                    FoundryCollaborationAgents.AdvisorInstructions),
                ContextHash = request.Surface.ContextHash,
                GlossaryVersion = glossary.Version,
                InputInteractionIds = InteractionIds(interactions),
                ProfileVersionIn = profile.Version,
                ProfileVersionOut = profile.Version,
                RawRequest = advice.PromptPreview,
                RawResponse = JsonSerializer.Serialize(
                    new { advice.Suggestions, advice.PreferredLayout },
                    JsonOptions),
                Approvals = approvals,
                LatencyMs = stopwatch.ElapsedMilliseconds,
            },
            cancellationToken);

        if (interactions.Count > 0)
        {
            await updateQueue.EnqueueAsync(
                new CollaborationProfileUpdateWorkItem(
                    user.Id,
                    interactions,
                    BuildUpdateContext(request)),
                cancellationToken);
        }

        return Results.Ok(new ObservationsResponse(
            user.Id,
            interactions.Count,
            "accepted",
            advice.PromptPreview,
            advice.Suggestions,
            advice.PreferredLayout));
    }

    private static CollaborationProfileUpdateContext BuildUpdateContext(
        ObservationsRequest request) =>
        new(
            string.Join(" / ", request.Surface.SurfacePath),
            request.Surface.Title,
            request.Surface.ViewState,
            request.Surface.Annotations,
            request.App.ItemCount,
            request.Controls,
            AssembledContext: request.Surface.AssembledContext,
            ContextHash: request.Surface.ContextHash,
            SessionId: request.SessionId);

    // --- Dev-only observability endpoints ------------------------------------------------

    private static async Task<IResult> ListRunsAsync(
        IAgentRunRecorder runRecorder,
        int? take,
        CancellationToken cancellationToken)
    {
        var runs = await runRecorder.ListAsync(Math.Clamp(take ?? 50, 1, 200), cancellationToken);
        return Results.Ok(runs);
    }

    private static async Task<IResult> GetRunAsync(
        string runId,
        IAgentRunRecorder runRecorder,
        CancellationToken cancellationToken)
    {
        var run = await runRecorder.GetAsync(runId, cancellationToken);
        return run is null ? Results.NotFound() : Results.Ok(run);
    }

    private static async Task<IResult> ListSessions(
        ClaimsPrincipal principal,
        AdaptiveTeamBuilderDbContext db,
        IInteractionLog interactionLog,
        CancellationToken cancellationToken)
    {
        var user = await ResolveUserAsync(principal, db, cancellationToken);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(interactionLog.ListSessions(user.Id));
    }

    private static async Task<IResult> GetSessionInteractionsAsync(
        string sessionId,
        ClaimsPrincipal principal,
        AdaptiveTeamBuilderDbContext db,
        IInteractionLog interactionLog,
        CancellationToken cancellationToken)
    {
        var user = await ResolveUserAsync(principal, db, cancellationToken);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var interactions = await interactionLog.ReadSessionAsync(
            user.Id,
            sessionId,
            cancellationToken);
        return Results.Ok(interactions);
    }

    public sealed record ReplayRequest(string SessionId, string? PromptOverride = null);

    /// <summary>
    /// Replays a stored session's interaction stream through the profile updater,
    /// optionally with a modified prompt, and returns the resulting run record
    /// (including the profile diff). Dev-only.
    /// </summary>
    private static async Task<IResult> ReplayAsync(
        ReplayRequest request,
        ClaimsPrincipal principal,
        AdaptiveTeamBuilderDbContext db,
        IInteractionLog interactionLog,
        CollaborationProfileUpdateBackgroundService updateService,
        CancellationToken cancellationToken)
    {
        var user = await ResolveUserAsync(principal, db, cancellationToken);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var interactions = await interactionLog.ReadSessionAsync(
            user.Id,
            request.SessionId,
            cancellationToken);
        if (interactions.Count == 0)
        {
            return Results.NotFound(new { error = "No interactions stored for that session." });
        }

        var surfacePath = interactions
            .Select(i => i.SurfacePath)
            .LastOrDefault(p => p is { Count: > 0 });
        var record = await updateService.ProcessAsync(
            new CollaborationProfileUpdateWorkItem(
                user.Id,
                interactions,
                new CollaborationProfileUpdateContext(
                    surfacePath is null ? null : string.Join(" / ", surfacePath),
                    null,
                    null,
                    null,
                    SessionId: request.SessionId,
                    Trigger: "manual-replay",
                    PromptOverride: request.PromptOverride)),
            cancellationToken);
        return Results.Ok(record);
    }

    private static IResult ListPersonas(SyntheticPersonaProvider personas) =>
        Results.Ok(personas.ListNames());

    /// <summary>
    /// Runs a scripted persona through the real observations pipeline (JSONL log,
    /// DB rows, reversal flags, profile updater) to pre-warm a believable belief
    /// profile before a demo. Dev-only.
    /// </summary>
    private static async Task<IResult> RunPersonaAsync(
        string name,
        ClaimsPrincipal principal,
        AdaptiveTeamBuilderDbContext db,
        SyntheticPersonaProvider personas,
        IInteractionLog interactionLog,
        CollaborationProfileUpdateBackgroundService updateService,
        CancellationToken cancellationToken)
    {
        var user = await ResolveUserAsync(principal, db, cancellationToken);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var script = personas.Get(name);
        if (script is null)
        {
            return Results.NotFound(new { error = $"No persona script named '{name}'." });
        }

        var contracts = await db.Contracts
            .AsNoTracking()
            .OrderBy(c => c.DemoSortOrder)
            .ThenBy(c => c.Code)
            .ToListAsync(cancellationToken);
        if (contracts.Count == 0)
        {
            return Results.NotFound(new { error = "No contracts available to script against." });
        }

        var sessionId = $"persona_{name}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        var turns = personas.Synthesize(script, contracts, sessionId);
        var runIds = new List<string>();

        foreach (var batch in turns)
        {
            var flagged = CollaborationContextFormatter.FlagReversals(batch);
            await interactionLog.AppendAsync(user.Id, sessionId, flagged, cancellationToken);
            await PersistInteractionsAsync(db, user.Id, sessionId, flagged, cancellationToken);

            var record = await updateService.ProcessAsync(
                new CollaborationProfileUpdateWorkItem(
                    user.Id,
                    flagged,
                    new CollaborationProfileUpdateContext(
                        "page:contracts / section:contracts.list",
                        "Select a contract",
                        null,
                        null,
                        SessionId: sessionId,
                        Trigger: "persona")),
                cancellationToken);
            runIds.Add(record.RunId);
        }

        return Results.Ok(new
        {
            persona = script.Name,
            sessionId,
            turnCount = turns.Count,
            interactionCount = turns.Sum(t => t.Count),
            runIds,
        });
    }

    // --- Helpers --------------------------------------------------------------------------

    private static AdviseResponse FilterApproved(
        AdviseResponse response,
        IReadOnlyList<AdaptationApprovalRecord> approvals)
    {
        var approvedIds = approvals
            .Where(a => a.Approved)
            .Select(a => a.AdaptationId)
            .ToHashSet(StringComparer.Ordinal);
        return response with
        {
            PreferredLayout = approvedIds.Contains("preferred-layout") ? response.PreferredLayout : null,
            Suggestions = response.Suggestions.Where(s => approvedIds.Contains(s.Id)).ToList(),
        };
    }

    private static IReadOnlyList<string> InteractionIds(IReadOnlyList<InteractionDto> interactions) =>
        interactions
            .Select(i => i.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToList();

    private static async Task PersistInteractionsAsync(
        AdaptiveTeamBuilderDbContext db,
        Guid userId,
        string sessionId,
        IReadOnlyList<InteractionDto> interactions,
        CancellationToken cancellationToken)
    {
        if (interactions.Count == 0)
        {
            return;
        }

        foreach (var dto in interactions)
        {
            db.Interactions.Add(new Interaction
            {
                UserId = userId,
                SessionId = string.IsNullOrWhiteSpace(dto.SessionId) ? sessionId : dto.SessionId,
                ClientInteractionId = dto.Id,
                Seq = dto.Seq,
                At = dto.At,
                SurfacePath = string.Join(" / ", dto.SurfacePath),
                ControlId = dto.ControlId,
                Action = dto.Action,
                ValueBefore = dto.ValueBefore,
                ValueAfter = dto.ValueAfter,
                Causation = string.IsNullOrWhiteSpace(dto.Causation) ? "user" : dto.Causation,
                Reversal = dto.Reversal == true,
                EntityJson = dto.Entity is null
                    ? null
                    : JsonSerializer.Serialize(dto.Entity, JsonOptions),
                ChoiceSetJson = dto.ChoiceSet is null
                    ? null
                    : JsonSerializer.Serialize(dto.ChoiceSet, JsonOptions),
                MetaJson = dto.Meta is null
                    ? null
                    : JsonSerializer.Serialize(dto.Meta, JsonOptions),
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task StampSelectedContractsAsync(
        AdaptiveTeamBuilderDbContext db,
        IReadOnlyList<InteractionDto> events,
        CancellationToken cancellationToken)
    {
        var selectedIds = events
            .Where(e => e.Action == "control.select" && !string.IsNullOrWhiteSpace(e.ControlId))
            .Select(e => Guid.TryParse(e.ControlId, out var id) ? id : (Guid?)null)
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        if (selectedIds.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var contracts = await db.Contracts
            .Where(c => selectedIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        foreach (var contract in contracts)
        {
            contract.LastSelectedAt = now;
            contract.ModifiedDate = now;
        }

        if (contracts.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task<AdaptiveTeamBuilder.Data.Entities.User?> ResolveUserAsync(
        ClaimsPrincipal principal,
        AdaptiveTeamBuilderDbContext db,
        CancellationToken cancellationToken)
    {
        if (!TryGetObjectId(principal, out var objectId))
        {
            return null;
        }

        return await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.AzureAdObjectId == objectId, cancellationToken);
    }

    private static bool TryGetObjectId(ClaimsPrincipal principal, out string objectId)
    {
        objectId =
            principal.FindFirst("oid")?.Value
            ?? principal.FindFirst(ObjectIdClaim)?.Value
            ?? string.Empty;

        return !string.IsNullOrWhiteSpace(objectId);
    }
}
