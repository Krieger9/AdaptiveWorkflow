using AdaptiveTeamBuilder.Data;
using AdaptiveTeamBuilder.Data.Contracts;
using AdaptiveTeamBuilder.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AdaptiveTeamBuilderSvc;

public interface IBeliefProfileStore
{
    /// <summary>Reads the control-tier belief document (seeded default when absent).</summary>
    Task<BeliefProfileDto> GetAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Persists a validated belief document: bumps the version, re-projects the Beliefs
    /// table (keyed by surface path + dimension), and archives a versioned markdown copy
    /// to data/profiles/{userId}/control.v{n}.md. Returns the new version.
    /// </summary>
    Task<int> SaveAsync(
        Guid userId,
        string document,
        string source,
        CancellationToken cancellationToken);

    Task<long?> AppendTurnDigestAsync(
        Guid userId,
        string surfacePath,
        string? digest,
        CancellationToken cancellationToken);

    /// <summary>
    /// Appends entries to the unified Revisions log (revisions AND challenges-that-held).
    /// </summary>
    Task AppendRevisionAsync(
        Guid userId,
        string surfacePath,
        string kind,
        string? reason,
        long? turnDigestId,
        CancellationToken cancellationToken);
}

public sealed class BeliefProfileStore(
    AdaptiveTeamBuilderDbContext db,
    IOptions<AgentFrameworkOptions> options,
    IHostEnvironment environment,
    ILogger<BeliefProfileStore> logger) : IBeliefProfileStore
{
    /// <summary>Number of most-recent digests surfaced to the updater/advisor prompts.</summary>
    public const int MaxRecentTurnDigests = 5;

    public async Task<BeliefProfileDto> GetAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var stored = await db.BeliefDocuments.AsNoTracking()
            .FirstOrDefaultAsync(
                d => d.UserId == userId && d.Tier == BeliefDocumentFormat.ControlTier,
                cancellationToken);

        // Newest N rows, then reversed to oldest->newest for prompt formatting.
        var recent = await db.TurnDigests.AsNoTracking()
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.Sequence)
            .Take(MaxRecentTurnDigests)
            .Select(d => d.DigestText)
            .ToListAsync(cancellationToken);
        recent.Reverse();

        if (stored is null)
        {
            return new BeliefProfileDto(
                BeliefDocumentFormat.ControlTier,
                BeliefDocumentFormat.CreateDefaultControlDocument(userId, DateTime.UtcNow),
                "app",
                0,
                null,
                recent);
        }

        return new BeliefProfileDto(
            stored.Tier,
            stored.Document,
            stored.Source,
            stored.Version,
            stored.UpdatedAt,
            recent);
    }

    public async Task<int> SaveAsync(
        Guid userId,
        string document,
        string source,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var stored = await db.BeliefDocuments
            .FirstOrDefaultAsync(
                d => d.UserId == userId && d.Tier == BeliefDocumentFormat.ControlTier,
                cancellationToken);

        if (stored is null)
        {
            stored = new BeliefDocument
            {
                UserId = userId,
                Tier = BeliefDocumentFormat.ControlTier,
                Version = 0,
            };
            db.BeliefDocuments.Add(stored);
        }

        stored.Document = document;
        stored.Source = source;
        stored.Version += 1;
        stored.UpdatedAt = now;

        await ProjectBeliefsAsync(userId, document, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        ArchiveVersion(userId, stored.Tier, stored.Version, document);
        return stored.Version;
    }

    public async Task<long?> AppendTurnDigestAsync(
        Guid userId,
        string surfacePath,
        string? digest,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(digest))
        {
            return null;
        }

        var nextSequence = await db.TurnDigests
            .Where(d => d.UserId == userId)
            .MaxAsync(d => (int?)d.Sequence, cancellationToken) ?? 0;

        var entity = new TurnDigest
        {
            UserId = userId,
            SurfacePath = surfacePath,
            Sequence = nextSequence + 1,
            CreatedAt = DateTime.UtcNow,
            DigestText = digest.Trim(),
        };
        db.TurnDigests.Add(entity);

        await db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task AppendRevisionAsync(
        Guid userId,
        string surfacePath,
        string kind,
        string? reason,
        long? turnDigestId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return;
        }

        db.Revisions.Add(new Revision
        {
            UserId = userId,
            SurfacePath = surfacePath,
            Kind = kind,
            Reason = reason.Trim(),
            TurnDigestId = turnDigestId,
            CreatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Re-projects the Beliefs rows (surface path + dimension) from the document.</summary>
    private async Task ProjectBeliefsAsync(
        Guid userId,
        string document,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var validation = BeliefDocumentFormat.Validate(document);
        var existing = await db.Beliefs
            .Where(b => b.UserId == userId)
            .ToListAsync(cancellationToken);

        foreach (var parsed in validation.Beliefs)
        {
            var row = existing.FirstOrDefault(b =>
                string.Equals(b.SurfacePath, parsed.SurfacePath, StringComparison.OrdinalIgnoreCase)
                && string.Equals(b.Dimension, parsed.Dimension, StringComparison.OrdinalIgnoreCase));

            if (row is null)
            {
                row = new Belief
                {
                    UserId = userId,
                    SurfacePath = parsed.SurfacePath,
                    Dimension = parsed.Dimension,
                };
                db.Beliefs.Add(row);
            }

            row.Statement = parsed.Statement;
            row.Conviction = parsed.Conviction;
            row.Tenure = parsed.Tenure;
            row.LeaningOn = parsed.LeaningOn;
            row.ChangeCriteria = parsed.ChangeCriteria;
            row.UpdatedAt = now;
        }

        var retired = existing.Where(b => !validation.Beliefs.Any(p =>
            string.Equals(p.SurfacePath, b.SurfacePath, StringComparison.OrdinalIgnoreCase)
            && string.Equals(p.Dimension, b.Dimension, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (retired.Count > 0)
        {
            db.Beliefs.RemoveRange(retired);
        }
    }

    /// <summary>
    /// Every write produces a new numbered version on disk. The diff history is the
    /// research dataset — never overwrite without archiving.
    /// </summary>
    private void ArchiveVersion(Guid userId, string tier, int version, string document)
    {
        try
        {
            var directory = Path.Combine(
                PathUtilities.Resolve(environment, options.Value.DataDirectory),
                "profiles",
                userId.ToString("D"));
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, $"{tier}.v{version}.md"), document);
            File.WriteAllText(Path.Combine(directory, $"{tier}.md"), document);
        }
        catch (IOException ex)
        {
            logger.LogWarning(
                ex,
                "Failed to archive belief document v{Version} for user {UserId}.",
                version,
                userId);
        }
    }
}
