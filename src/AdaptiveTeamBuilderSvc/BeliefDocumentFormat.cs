using System.Text;
using System.Text.RegularExpressions;

namespace AdaptiveTeamBuilderSvc;

/// <summary>One belief section parsed out of a belief document.</summary>
public sealed record ParsedBelief(
    string SurfacePath,
    string Dimension,
    string Statement,
    string Conviction,
    string Tenure,
    string LeaningOn,
    string ChangeCriteria);

public sealed record BeliefDocumentValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    int ChangelogEntryCount,
    IReadOnlyList<ParsedBelief> Beliefs);

/// <summary>One changelog entry parsed out of a belief document.</summary>
public sealed record ParsedChangelogEntry(string Heading, string Kind, string Body);

/// <summary>
/// The profile is a markdown document the agent maintains via read-modify-write.
/// The code does not compute beliefs, but it validates document shape on write:
/// every belief section has all five fields (Belief, Tenure, Conviction,
/// What I'm leaning on, What would change my mind), conviction is one of the five
/// levels, and every write appends at least one changelog entry.
/// </summary>
public static partial class BeliefDocumentFormat
{
    public const string ControlTier = "control";

    /// <summary>Ordered conviction levels. The agent assigns these; nothing in code derives them.</summary>
    public static readonly IReadOnlyList<string> ConvictionLevels =
        ["noticed", "tentative", "working theory", "settled", "entrenched"];

    /// <summary>Changelog entry kinds a heading must state.</summary>
    public static readonly IReadOnlyList<string> ChangelogKinds =
        ["revised", "challenged", "created", "retired", "proposed"];

    /// <summary>Surface scope of the contracts list, used by the seeded document.</summary>
    public const string ContractsListScope = "section:contracts.list";

    [GeneratedRegex(@"^##\s+(?<scope>[^·\n]+?)\s*·\s*(?<dimension>[^\n]+?)\s*$", RegexOptions.Multiline)]
    private static partial Regex BeliefHeadingRegex();

    [GeneratedRegex(@"^###\s+(?<heading>[^\n]+)$", RegexOptions.Multiline)]
    private static partial Regex ChangelogEntryRegex();

    /// <summary>
    /// Seeded control-tier document. Wording preserves today's demo defaults
    /// (numeric values, collapsed summary cards, expand for extended detail before selecting).
    /// </summary>
    public static string CreateDefaultControlDocument(Guid userId, DateTime utcNow)
    {
        var date = utcNow.ToString("yyyy-MM-dd");
        return $"""
# Control-Tier Profile — user:{userId}
_Last revised {date} by app seed_

---

## {ContractsListScope} · information-form

**Belief:** No observed preference yet; the app default is numeric signal values (`bare`).

**Tenure:** seeded {date} · no observations yet · challenged 0 times
**Conviction:** noticed

**What I'm leaning on:** Nothing yet — this is the application default, not evidence.

**What would change my mind:** Any repeated user switch to the relative graph display, or sustained use of values after trying graph.

---

## {ContractsListScope} · disclosure-default

**Belief:** No observed preference yet; the app default is `collapsed` summary cards. Expand a card for extended staffing/scope detail before selecting.

**Tenure:** seeded {date} · no observations yet · challenged 0 times
**Conviction:** noticed

**What I'm leaning on:** Nothing yet — this is the application default, not evidence.

**What would change my mind:** Repeated expansion of most cards (toward `expanded`), or consistent selection from summary without expanding (toward staying `collapsed`).

---

## {ContractsListScope} · selection-rule

**Belief:** No rule yet for which contracts this user inspects first. No preferred commercial signal or graph-vs-values preference yet.

**Tenure:** seeded {date} · no observations yet · challenged 0 times
**Conviction:** noticed

**What I'm leaning on:** Nothing yet.

**What would change my mind:** Consistent first expansions matching a rule over the choice set (e.g. the two strongest contracts on one commercial signal), including what was NOT chosen.

---

## {ContractsListScope} · metric-attention

**Belief:** No signal ordering yet; all commercial signals are merely present.

**Tenure:** seeded {date} · no observations yet · challenged 0 times
**Conviction:** noticed

**What I'm leaning on:** Nothing yet.

**What would change my mind:** Repeated inspection or activation of specific signals (e.g. Margin, Profit) versus the rest.

---

## Changelog

### {date} · created seeded beliefs
Seeded the four glossary dimensions for {ContractsListScope} at `noticed` with app defaults. No user evidence yet.
""";
    }

    /// <summary>Validates document shape per the framework invariants (§5.4).</summary>
    public static BeliefDocumentValidationResult Validate(string document)
    {
        var errors = new List<string>();
        var beliefs = new List<ParsedBelief>();

        if (string.IsNullOrWhiteSpace(document))
        {
            return new BeliefDocumentValidationResult(false, ["Document is empty."], 0, beliefs);
        }

        var changelogIndex = document.IndexOf("## Changelog", StringComparison.OrdinalIgnoreCase);
        if (changelogIndex < 0)
        {
            errors.Add("Missing '## Changelog' section.");
        }

        var beliefRegion = changelogIndex >= 0 ? document[..changelogIndex] : document;
        var changelogRegion = changelogIndex >= 0 ? document[changelogIndex..] : string.Empty;

        var headings = BeliefHeadingRegex().Matches(beliefRegion);
        if (headings.Count == 0)
        {
            errors.Add("No belief sections found (expected '## <surface-scope> · <dimension>' headings).");
        }

        for (var i = 0; i < headings.Count; i++)
        {
            var match = headings[i];
            var scope = match.Groups["scope"].Value.Trim();
            var dimension = match.Groups["dimension"].Value.Trim().Trim('`');
            var start = match.Index + match.Length;
            var end = i + 1 < headings.Count ? headings[i + 1].Index : beliefRegion.Length;
            var body = beliefRegion[start..end];

            var statement = ExtractField(body, "Belief");
            var tenure = ExtractField(body, "Tenure");
            var conviction = ExtractField(body, "Conviction");
            var leaningOn = ExtractField(body, "What I'm leaning on");
            var changeCriteria = ExtractField(body, "What would change my mind");

            var sectionName = $"{scope} · {dimension}";
            if (statement is null) errors.Add($"Section '{sectionName}' is missing '**Belief:**'.");
            if (tenure is null) errors.Add($"Section '{sectionName}' is missing '**Tenure:**'.");
            if (conviction is null) errors.Add($"Section '{sectionName}' is missing '**Conviction:**'.");
            if (leaningOn is null) errors.Add($"Section '{sectionName}' is missing '**What I'm leaning on:**'.");
            if (changeCriteria is null) errors.Add($"Section '{sectionName}' is missing '**What would change my mind:**'.");

            if (conviction is not null)
            {
                var level = conviction.Trim().Trim('`').ToLowerInvariant();
                if (!ConvictionLevels.Contains(level))
                {
                    errors.Add(
                        $"Section '{sectionName}' has conviction '{conviction}'; expected one of: "
                        + string.Join(", ", ConvictionLevels) + ".");
                }
            }

            beliefs.Add(new ParsedBelief(
                scope,
                dimension,
                statement ?? string.Empty,
                (conviction ?? "noticed").Trim().Trim('`').ToLowerInvariant(),
                tenure ?? string.Empty,
                leaningOn ?? string.Empty,
                changeCriteria ?? string.Empty));
        }

        var changelogEntries = ParseChangelogEntries(changelogRegion);
        if (changelogIndex >= 0 && changelogEntries.Count == 0)
        {
            errors.Add("Changelog has no entries (expected '### <date> · <kind> ...' headings).");
        }

        foreach (var entry in changelogEntries)
        {
            if (string.IsNullOrEmpty(entry.Kind))
            {
                errors.Add(
                    $"Changelog entry '{entry.Heading}' does not state what happened "
                    + $"(expected one of: {string.Join(", ", ChangelogKinds)}).");
            }
        }

        return new BeliefDocumentValidationResult(
            errors.Count == 0,
            errors,
            changelogEntries.Count,
            beliefs);
    }

    /// <summary>Parses changelog entries from the region at/after '## Changelog'.</summary>
    public static IReadOnlyList<ParsedChangelogEntry> ParseChangelogEntries(string changelogRegion)
    {
        var entries = new List<ParsedChangelogEntry>();
        if (string.IsNullOrWhiteSpace(changelogRegion))
        {
            return entries;
        }

        var matches = ChangelogEntryRegex().Matches(changelogRegion);
        for (var i = 0; i < matches.Count; i++)
        {
            var heading = matches[i].Groups["heading"].Value.Trim();
            var start = matches[i].Index + matches[i].Length;
            var end = i + 1 < matches.Count ? matches[i + 1].Index : changelogRegion.Length;
            var body = changelogRegion[start..end].Trim();
            var kind = ChangelogKinds.FirstOrDefault(k =>
                heading.Contains(k, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
            entries.Add(new ParsedChangelogEntry(heading, kind, body));
        }

        return entries;
    }

    /// <summary>Extracts the changelog region of a document ('' when absent).</summary>
    public static string ChangelogRegion(string document)
    {
        var index = document.IndexOf("## Changelog", StringComparison.OrdinalIgnoreCase);
        return index < 0 ? string.Empty : document[index..];
    }

    /// <summary>
    /// Minimal unified line diff between two documents (for run records; not a general tool).
    /// </summary>
    public static string UnifiedDiff(string before, string after)
    {
        var a = before.Replace("\r\n", "\n").Split('\n');
        var b = after.Replace("\r\n", "\n").Split('\n');

        // LCS table (documents are small; O(n*m) is fine here).
        var lcs = new int[a.Length + 1, b.Length + 1];
        for (var i = a.Length - 1; i >= 0; i--)
        {
            for (var j = b.Length - 1; j >= 0; j--)
            {
                lcs[i, j] = a[i] == b[j]
                    ? lcs[i + 1, j + 1] + 1
                    : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
            }
        }

        var sb = new StringBuilder();
        int x = 0, y = 0;
        while (x < a.Length && y < b.Length)
        {
            if (a[x] == b[y])
            {
                x++;
                y++;
            }
            else if (lcs[x + 1, y] >= lcs[x, y + 1])
            {
                sb.Append("- ").AppendLine(a[x++]);
            }
            else
            {
                sb.Append("+ ").AppendLine(b[y++]);
            }
        }

        while (x < a.Length)
        {
            sb.Append("- ").AppendLine(a[x++]);
        }

        while (y < b.Length)
        {
            sb.Append("+ ").AppendLine(b[y++]);
        }

        return sb.ToString().TrimEnd();
    }

    private static string? ExtractField(string sectionBody, string fieldName)
    {
        var marker = $"**{fieldName}:**";
        var index = sectionBody.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return null;
        }

        var start = index + marker.Length;
        // Field value runs until the next bold field marker or the end of the section.
        var next = sectionBody.IndexOf("\n**", start, StringComparison.Ordinal);
        var end = next < 0 ? sectionBody.Length : next;
        return sectionBody[start..end].Trim();
    }

    /// <summary>
    /// Replaces one field's value inside the '## scope · dimension' section of a document.
    /// Returns the document unchanged when the section or field is not found.
    /// </summary>
    public static string ReplaceBeliefField(
        string document,
        string scope,
        string dimension,
        string fieldName,
        string newValue)
    {
        var headings = BeliefHeadingRegex().Matches(document);
        for (var i = 0; i < headings.Count; i++)
        {
            var match = headings[i];
            if (!string.Equals(match.Groups["scope"].Value.Trim(), scope, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(
                    match.Groups["dimension"].Value.Trim().Trim('`'),
                    dimension,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var start = match.Index + match.Length;
            var end = i + 1 < headings.Count ? headings[i + 1].Index : document.Length;
            var changelogIndex = document.IndexOf("## Changelog", start, StringComparison.OrdinalIgnoreCase);
            if (changelogIndex >= 0 && changelogIndex < end)
            {
                end = changelogIndex;
            }

            var body = document[start..end];
            var marker = $"**{fieldName}:**";
            var fieldIndex = body.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (fieldIndex < 0)
            {
                return document;
            }

            var valueStart = fieldIndex + marker.Length;
            var next = body.IndexOf("\n**", valueStart, StringComparison.Ordinal);
            var separator = body.IndexOf("\n---", valueStart, StringComparison.Ordinal);
            var valueEnd = next < 0 ? body.Length : next;
            if (separator >= 0 && separator < valueEnd)
            {
                valueEnd = separator;
            }

            var newBody = body[..valueStart] + " " + newValue.Trim() + "\n" + body[valueEnd..].TrimStart('\n');
            return document[..start] + newBody + document[end..];
        }

        return document;
    }

    /// <summary>Appends a changelog entry ('### date · kind ...' + body) to the document.</summary>
    public static string AppendChangelogEntry(string document, string heading, string body)
    {
        var trimmed = document.TrimEnd();
        if (!trimmed.Contains("## Changelog", StringComparison.OrdinalIgnoreCase))
        {
            trimmed += "\n\n## Changelog";
        }

        return trimmed + $"\n\n### {heading.Trim()}\n{body.Trim()}\n";
    }
}
