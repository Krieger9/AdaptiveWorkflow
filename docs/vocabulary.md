# Vocabulary — one page, 1:1 everywhere

Every term in the adaptive UI preference framework maps 1:1 to a code type, a database
table (where persisted), and the phrase used in agent prompts. No scores, weights, or
thresholds appear anywhere in code or prompts.

| Term | Meaning | Code type | DB table | Prompt phrase |
|---|---|---|---|---|
| **Interaction** | One logged user or system act against a surface; the atomic unit of evidence. | `Interaction` (TS), `InteractionDto` / `Interaction` entity (C#) | `dbo.Interactions` (+ JSONL log per session) | "interaction" |
| **Surface** | A place in the UI where preferences can express themselves; identified as `kind:dotted.name`. | `<Surface>` component / `SurfaceNode` (TS) | — (registry is in-memory; path is persisted on interactions/beliefs) | "surface" |
| **Surface path** | Ordered surface ids from root to leaf; the scope key for beliefs and prompts. | `surfacePath: string[]` (TS), `SurfacePath` (C#) | `Interactions.SurfacePath`, `Beliefs.SurfacePath` | "surface" / scope line in context |
| **Causation** | Who caused an interaction: `user`, `agent-applied`, or `restored`. | `Causation` (TS union / C# string) | `Interactions.Causation` | "causation" |
| **Reversal** | A user act that undoes a recent agent-applied state; the strongest signal. | `reversal: boolean` on `Interaction` | `Interactions.Reversal` | "REVERSAL" marker |
| **Choice set** | The alternatives visible at the moment of an interaction; enables reasoning about what was *not* chosen. | `ChoiceSetItem[]` (TS), `ChoiceSet` (C#) | `Interactions.ChoiceSet` (JSON) | "choice set" / "not chosen" |
| **Dimension** | A named axis of preference from the glossary (e.g. `information-form`, `disclosure-default`). | `Dimension` fields; `data/glossary.json` | `Beliefs.Dimension` | "dimension" |
| **Belief** | The agent's current working theory about the user on one dimension at one scope. | `Belief` entity (C#); belief section in document | `dbo.Beliefs` (projection of the document) | "**Belief:**" field |
| **Belief document** | The markdown profile per tier: Belief / Tenure / Conviction / What I'm leaning on / What would change my mind / Changelog. | `BeliefProfile.document` (TS), `BeliefDocument` entity (C#) | `dbo.BeliefDocuments` (+ versioned files `data/profiles/{userId}/control.v{n}.md`) | the document itself |
| **Conviction** | How settled a belief is: noticed → tentative → working theory → settled → entrenched. | `Conviction` field on `Belief` | `Beliefs.Conviction` | "**Conviction:**" field |
| **Tenure** | How long a belief has held, in plain words (e.g. "since first session"). | `Tenure` field on `Belief` | `Beliefs.Tenure` | "**Tenure:**" field |
| **Turn digest** | One-sentence summary of a decision turn, kept as recent context. | `recentTurnDigests` (TS), `TurnDigest` entity (C#) | `dbo.TurnDigests` | "recent decision digests" |
| **Revision** | A logged belief change — or a challenge that held (belief kept despite contrary evidence). | `Revision` entity (C#); changelog entries (`revised` / `challenged` / `created` / `retired` / `proposed`) | `dbo.Revisions` | "## Changelog" entries |
| **Suggestion** | An adaptation the advisor proposes for the current surface. | `CollaborationSuggestion` (TS), `SuggestionDto` (C#) | — (run records on disk) | "suggestion" |
| **Approval** | The decision point between agent output and UI application; currently `AutoApprove`, logged per adaptation. | `IAdaptationApprovalPolicy` (C#) | — (`data/runs/*.json` approvals) | — (not in prompts) |
