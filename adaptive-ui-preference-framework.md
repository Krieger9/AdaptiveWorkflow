# Adaptive UI Preference Framework — Desired State Design

**Status:** Design target for Round 2 POC
**Audience:** Coding agent implementing against the existing contracts-staffing sample app
**Document purpose:** Describes the system as it should exist when complete. Not a migration plan.

---

## 0. Intent and Non-Goals

### What this is

An experiment in whether LLM agents, given well-annotated UI interaction data and hierarchical
context, can infer meaningful user preferences — and can hold, revise, and explain those beliefs
over time in a way that is legible to us.

The agents are the subject of study. The surrounding code exists to feed them honest data,
structure their memory, and make their reasoning observable.

### What this is not

- **Not production-ready.** No auth hardening, no scale work, no multi-tenant concerns.
- **Not deterministic.** Belief state, conviction, and stickiness live in agent prompts and
  agent-maintained documents. We are explicitly *not* moving these into code. Doing so would
  measure our own arithmetic instead of the model's judgment.
- **Not precision-optimized.** Fuzziness is acceptable. Wrong inferences that surface as
  dismissable suggestions cost almost nothing and generate signal.

### Explicit non-goals for Round 2

- Cross-application universal profiles (tier is scaffolded, not populated — see §7.3)
- Ontology governance across teams
- Privacy/export/deletion tooling beyond "the profile is a readable file"
- Any automatic application of preferences without user confirmation (see §8)

### Success criteria

We consider Round 2 successful if, after several weeks of use, we can answer:

1. Does the agent's stated conviction level correlate with anything real, or is it decorative?
2. Do agents converge on similar invented vocabulary across different users and controls?
3. Does the tiered architecture produce genuine abstraction, or does the app tier just restate
   the control tier?
4. What is the actual reversal latency for a long-held belief, and how variable is it?

Answering these requires the observability harness (§10). **The harness is the primary
deliverable, not a nice-to-have.**

---

## 1. Locked Vocabulary

These terms have fixed meaning throughout the code, the prompts, and the profile documents.
The coding agent must use them consistently in type names, file names, and prompt text.

| Term | Definition |
|---|---|
| **Surface** | A region of UI that declares a purpose. Surfaces nest. The surface tree is the context hierarchy. |
| **Surface Path** | Ordered list of surface IDs from root to leaf, e.g. `page:staffing / section:contracts.list / group:contract.card`. Semantic, never DOM-derived. |
| **Interaction** | One logged user or system act against a surface. The atomic unit of evidence. |
| **Choice Set** | The alternatives that were visible and available at the moment of an interaction, with their attribute values. Enables negatives. |
| **Causation** | Why a state change happened: `user`, `system-default`, `restored`, `agent-applied`. |
| **Dimension** | A named axis of preference, e.g. `information-form`. Has a defined value set. May be seeded or agent-proposed. |
| **Belief** | The agent's current position on one dimension within one scope, held in the profile document. |
| **Conviction** | How firmly a belief is held. One of five ordered levels (§5.2). Agent-assigned. |
| **Tenure** | How long a belief has been held, and how many times it has been challenged and survived. |
| **Challenge** | An observation that contradicts a held belief. Challenges are counted, whether or not they cause revision. |
| **Revision** | A change to a belief, recorded in the profile changelog with the agent's stated reasoning. |
| **Suggestion** | A user-facing offer to change UI behavior based on a belief. The only path by which beliefs affect the UI. |
| **Probe** | A suggestion issued deliberately to resolve agent uncertainty rather than because confidence is high. |
| **Glossary** | The app-supplied set of dimensions and their value definitions, expressed as observable behavior. |

### 1.1 Terms deliberately avoided

Do not use in code or prompts: *score*, *weight*, *confidence value*, *threshold*, *decay rate*.
These imply numeric machinery we are not building and will bias the agent toward false precision.
Use *conviction*, *tenure*, *challenge* instead.

---

## 2. The Glossary

The glossary is app-supplied data, not code. It defines dimensions **by observable behavior**,
so that agents and humans can agree on what a value means without a shared numeric scale.

### 2.1 Format

```json
{
  "version": "2026-08-14",
  "dimensions": [
    {
      "id": "information-form",
      "description": "How a numeric metric is rendered.",
      "values": {
        "bare": "Numeric value only, no visual encoding.",
        "trended": "Numeric value with an inline sparkline beside it.",
        "charted": "A chart replaces the numeric value as the primary representation."
      },
      "origin": "seeded"
    },
    {
      "id": "disclosure-default",
      "description": "Whether a collapsible region starts open.",
      "values": {
        "collapsed": "Region starts closed; user opens what they want.",
        "expanded": "Region starts open; user closes what they don't want.",
        "selective": "Some subset starts open, chosen by a rule over the data."
      },
      "origin": "seeded"
    },
    {
      "id": "selection-rule",
      "description": "The predicate that explains which items a user chooses to inspect, when they inspect only some.",
      "values": "open — the agent describes the rule in prose, e.g. 'the two highest-margin contracts'",
      "origin": "seeded"
    },
    {
      "id": "metric-attention",
      "description": "Which metrics the user actually acts on, versus which are merely present.",
      "values": "open — the agent names metrics in priority order",
      "origin": "seeded"
    }
  ]
}
```

### 2.2 Seed set

Ship exactly the four above. Resist adding more. The point is to see what the agents invent.

### 2.3 Agent-proposed dimensions

Agents may define new dimensions. A proposed dimension must include:

- `id` — kebab-case
- `description` — one sentence
- `values` — either an enumerated map with behavioral definitions, or `"open"` with a note on
  what form the prose answer takes
- `confirming_behavior` — what would make this more likely
- `disconfirming_behavior` — what would make this less likely
- `origin: "proposed"`, plus `proposed_by` (tier) and `proposed_on` (date)

Proposed dimensions are written to a separate `proposed-dimensions.json` and are **live
immediately** — the agent may hold beliefs against them. Promotion into the main glossary is a
human decision made later, informed by whether the same dimension recurs across users.

> **Design note for the implementer:** the `disconfirming_behavior` field is load-bearing. It
> forces the agent to make each proposed dimension falsifiable at the moment it invents it.
> Do not drop it for brevity.

---

## 3. Surface Annotation Contract

### 3.1 React API

```tsx
<Surface
  id="page:staffing"
  purpose="Staffing lead decides which open contracts need consultants assigned this week.
           Time-pressured; scanning for problems rather than browsing."
  domain="Contract: a signed client engagement.
          Margin = (billRate - cost) / billRate.
          Fill rate = staffed roles / required roles.
          Low fill rate on high-margin work is the thing that hurts the business."
>
  <Surface
    id="section:contracts.list"
    purpose="Triage. Which contracts need attention right now?"
  >
    <Surface
      id="group:contract.card"
      purpose="One contract: summary metrics, expandable to role-level detail."
      entity={{ type: "Contract", id: contract.id, attrs: {
        margin: contract.margin,
        value: contract.totalValue,
        fillRate: contract.fillRate,
        daysToStart: contract.daysToStart
      }}}
    >
```

**Rules:**

- `purpose` and `domain` are prose written for a reader, not a parser. Full sentences. They may
  be several lines. They are the highest-leverage input in the system.
- `domain` is optional and inherited. Declare it once at page level; deeper surfaces add domain
  detail only when the sub-region has vocabulary of its own.
- `entity` supplies the attribute snapshot used for choice sets and selection rules. Required on
  any surface representing a data item the user might select among.
- `id` uses a `kind:dotted.name` convention. Kinds: `page`, `section`, `group`, `control`.

### 3.2 Control instrumentation

```tsx
const { record } = useSurface();

<button onClick={() => {
  setExpanded(true);
  record({
    controlId: "disclosure-toggle",
    action: "expand",
    valueBefore: "collapsed",
    valueAfter: "expanded",
    causation: "user"
  });
}}>
```

Where possible, wrap common controls so the app author does not hand-write `record` calls:

```tsx
<AdaptiveDisclosure controlId="detail" expanded={x} onChange={...} />
<AdaptiveMetric controlId="margin" form={form} value={c.margin} history={c.marginHistory} />
```

These wrappers emit correctly-shaped interactions automatically, including causation.

### 3.3 Choice set capture

When an interaction targets one item among several visible ones, the choice set must be
captured. Provide a hook that reads sibling `entity` declarations from the surface tree:

```tsx
const { record, siblingEntities } = useSurface();
// record() attaches siblingEntities as choiceSet automatically when entity is present
```

**This is not optional.** Without the alternatives, "expands the highest-margin contracts" is
unfalsifiable — every hypothesis fits the positives alone. The choice set is what lets the agent
notice its own confounds (e.g. margin correlating with contract size in this dataset).

---

## 4. Interaction Record

### 4.1 Schema

```ts
interface Interaction {
  id: string;
  ts: string;              // ISO
  sessionId: string;
  seq: number;             // monotonic within session

  surfacePath: string[];   // ["page:staffing", "section:contracts.list", "group:contract.card"]
  controlId: string;
  action: string;          // "expand" | "collapse" | "change-form" | "sort" | "dismiss" | ...

  valueBefore?: string;
  valueAfter?: string;

  causation: "user" | "system-default" | "restored" | "agent-applied";

  entity?: { type: string; id: string; attrs: Record<string, number | string> };
  choiceSet?: Array<{ id: string; attrs: Record<string, number | string> }>;

  dwellMs?: number;
  latencyFromRenderMs?: number;

  // populated only on suggestion responses
  suggestionId?: string;
  suggestionResponse?: "accept" | "reject" | "amend";
  amendText?: string;
}
```

### 4.2 Causation rules

The `causation` field prevents the system's most damaging failure mode: the agent suggests a
change, the user doesn't undo it, and the agent reads that inaction as confirmation of a belief
it invented.

- Only `causation: "user"` interactions are evidence of preference.
- `agent-applied` and `restored` states are recorded for context but must be explicitly described
  to the agent as *not* user choices.
- A `user` interaction that reverses a recent `agent-applied` change is the single most
  informative event in the stream. The event assembler must flag these as `reversal: true` and
  the tier-1 prompt must call them out.

### 4.3 Storage

Append-only JSONL per session: `data/interactions/{userId}/{sessionId}.jsonl`.

Never mutate. Replay depends on this.

---

## 5. Profile Document

### 5.1 Format

The profile is a **markdown document the agent maintains via read-modify-write**. It is not a
serialized data structure with a rendering layer. The agent reads the markdown, writes new
markdown. Humans read the same text.

One file per scope tier:

```
data/profiles/{userId}/control.md      # tier 1
data/profiles/{userId}/application.md  # tier 2
data/profiles/{userId}/universal.md    # tier 3 (scaffolded, see §7.3)
```

### 5.2 Structure

```markdown
# Control-Tier Profile — user:krieger
_Last revised 2026-08-14 by tier-1 after session s_8841_

---

## section:contracts.list · disclosure-default

**Belief:** Prefers `collapsed`, opening items selectively rather than browsing all.

**Tenure:** held since 2026-03-02 (5 months) · observed across ~90 sessions ·
challenged 2 times, held both times
**Conviction:** entrenched

**What I'm leaning on:** Collapses restored panels within a few seconds of page load.
Has never used "expand all" despite it being present. On the two occasions he expanded
more than three, both were followed by an immediate collapse of most of them.

**What would change my mind:** Sustained expansion of most contracts across several
consecutive sessions, without follow-up collapses.

---

## section:contracts.list · selection-rule

**Belief:** Expands the two or three highest-margin contracts first and often stops there.

**Tenure:** held since 2026-06-14 (2 months) · seen in 12 of 15 observed sessions ·
challenged 1 time
**Conviction:** working theory

**What I'm leaning on:** In 12 sessions the first two expansions were the top-two-by-margin
items in the visible set.

**Doubt:** Margin correlates strongly with total contract value in this data, so I cannot
yet separate "highest margin" from "biggest contract." One session on 2026-08-09 is weak
evidence for margin — he skipped a high-value, low-margin contract — but that is a single
observation. A probe would settle this.

---

## Changelog

### 2026-08-14 · revised `selection-rule`
Raised from `tentative` to `working theory`. Three consecutive sessions matched the
top-two-by-margin pattern. Noting explicitly that I have not ruled out contract size as
the real driver; raising conviction on the *pattern*, not on the *cause*.

### 2026-08-11 · challenged `disclosure-default`, held
He expanded six contracts in one session, which contradicts a collapsed default. But the
session was 40 minutes and looked like a review pass rather than triage, and he collapsed
four of them before leaving. A five-month belief challenged once by an atypical session
does not move. Counting the challenge; conviction unchanged at `entrenched`.

### 2026-08-04 · proposed new dimension `reversal-tolerance`
See proposed-dimensions.json. He undoes agent-applied changes almost immediately when they
are wrong, rather than living with them — which suggests a low tolerance for the UI moving
under him, and argues for suggesting rather than applying.
```

### 5.3 Conviction levels

Ordered, five levels. The agent assigns these; nothing in code derives them.

| Level | Meaning |
|---|---|
| `noticed` | Seen once or twice. Recording it, not acting on it. |
| `tentative` | A pattern that might be real. Not yet suggestion-worthy. |
| `working theory` | Consistent enough to act on. Suggestion-worthy. Still expects to be wrong sometimes. |
| `settled` | Reliable. Suggestions from this should feel obvious to the user. |
| `entrenched` | Long-held and repeatedly survived challenge. Requires sustained contradiction to move. |

### 5.4 Invariants the code enforces

The code does not compute beliefs, but it does validate document shape on write:

- Every belief section has all five fields (Belief, Tenure, Conviction, What I'm leaning on,
  What would change my mind).
- Conviction is one of the five levels.
- Every write appends at least one changelog entry.
- Every changelog entry states which belief it touched and what happened
  (`revised` / `challenged, held` / `created` / `retired` / `proposed`).

On validation failure: reject the write, log the raw agent output to the harness, retry once
with the validation error appended to the prompt. On second failure, keep the prior profile and
flag the session in the harness. **Do not silently accept malformed profiles** — a corrupted
profile poisons every subsequent revision.

### 5.5 Versioning

Every write produces a new numbered version: `control.md`, `control.v41.md`, etc. The diff
history *is* the research dataset. Never overwrite without archiving.

---

## 6. Context Assembly

Prompt context is generated from the surface tree, not hand-written per app. This is the
reusability mechanism.

### 6.1 Assembled context block

```
## Application context

### page:staffing
Staffing lead decides which open contracts need consultants assigned this week.
Time-pressured; scanning for problems rather than browsing.

Domain: Contract: a signed client engagement. Margin = (billRate - cost) / billRate.
Fill rate = staffed roles / required roles. Low fill rate on high-margin work is the
thing that hurts the business.

  ### section:contracts.list
  Triage. Which contracts need attention right now?

    ### group:contract.card
    One contract: summary metrics, expandable to role-level detail.
    Controls: disclosure-toggle (collapsed|expanded), metric-form (bare|trended|charted)
```

### 6.2 Assembly rules

- Depth-first traversal of the surface tree, indented by depth.
- Only surfaces touched by the interaction window are included, plus all their ancestors.
- `domain` text is inherited and printed once at the shallowest surface that declares it.
- The assembled tree is deterministic given the same surface registry — hash it and log the
  hash on every agent call so the harness can tell context changes from prompt changes.

---

## 7. Agent Tiers

Three agents. Each reads only the tier below's *output*, never raw events from a lower tier.
That constraint is what forces genuine abstraction at each boundary.

| Tier | Trigger | Reads | Writes |
|---|---|---|---|
| **1 — Control** | every ~20 user interactions, or on idle > 60s | assembled context, interaction window, current control profile, glossary | `control.md` |
| **2 — Application** | session end | assembled context, `control.md` (full), session shape summary, current app profile | `application.md`, proposed dimensions |
| **3 — Universal** | weekly | `application.md` from all apps | `universal.md` |

### 7.1 Tier 1 — Control Agent

**System prompt (desired state):**

```
You observe how one person uses a specific part of a user interface, and you maintain
beliefs about their preferences.

You will receive:
- Context describing what this part of the app is for and what the domain terms mean
- A window of recent interactions
- Your current profile document
- The glossary of preference dimensions

## Reading interactions

Only interactions with causation "user" are evidence of what this person prefers.
Interactions marked "agent-applied", "restored", or "system-default" are states the system
produced. If the user did not change something the system did, that is NOT agreement — it may
be inattention. Never treat inaction on a system-produced state as confirmation.

An interaction flagged `reversal: true` means the user undid something the system did. These
are your strongest signals. Weight them accordingly and say so.

When an interaction includes a choiceSet, pay attention to what was NOT chosen. A rule that
explains the choices but does not exclude the non-choices is not yet a rule. If two attributes
correlate in the available data, say plainly that you cannot separate them.

## Revising beliefs

Conviction levels, in order: noticed, tentative, working theory, settled, entrenched.

Before revising any belief, state how long you have held it and how many times it has been
challenged. Then reason explicitly about whether this new evidence is enough to move it.

A belief at `entrenched` requires sustained contradiction across multiple sessions — not one
session of counterexamples, however striking. But no belief is permanent. If contradiction has
persisted across several sessions, revise it, and say so plainly rather than hedging. An
entrenched belief that has been contradicted for a month is simply wrong and should be replaced.

If you decide a challenge is not enough to move a belief, still record the challenge and say
why it did not move you.

## Inventing dimensions

If you observe a consistent tendency that does not fit any dimension in the glossary, define a
new one. Give it an id, a one-sentence description, a definition of its values in terms of
observable behavior, and — required — what behavior would DISCONFIRM it. Mark it proposed.

## Output

Return the complete updated profile document in the exact format shown in your current profile.
Every revision requires a changelog entry stating what you changed and why. Write the changelog
for a human colleague who wants to understand your reasoning, not for a log parser.

Be willing to say you do not know. "I have two hypotheses and cannot separate them" is a more
useful profile entry than a confident guess.
```

### 7.2 Tier 2 — Application Agent

Reads the full control profile plus a session-shape summary (duration, surfaces visited, order,
interaction counts). Its job is abstraction across controls.

**Key prompt additions:**

```
You do not see raw interactions. You see what the control-tier agent concluded, and the shape
of the session.

Your job is to find patterns that span multiple controls. If the user prefers charted metrics
in three separate places, that is not three beliefs — it is one belief about how this person
reads quantitative information, and it should predict their behavior in a control you have
never seen.

Do not restate control-tier beliefs. If you have nothing to abstract, say so and write nothing.
A short honest profile is better than a padded one.

You may hand a hypothesis downward: if you believe something general, name the specific
controls where it predicts behavior that has not been observed yet. The control agent will
treat these as starting assumptions to test.

You may also propose probes — questions the system could ask the user that would resolve an
ambiguity you cannot settle by observation.
```

### 7.3 Tier 3 — Universal Agent

**Scaffold only for Round 2.** Implement the file, the trigger, and the prompt, but expect it to
have one app to read from and therefore little to say. It exists so that the tier boundary is
real from day one rather than retrofitted.

Its prompt should emphasize portability: a universal trait must be stated in terms that make
sense in an application it has never seen.

### 7.4 Shadow counter (instrumentation only)

Alongside the agents, maintain a trivial per-dimension observation counter — occurrences for,
occurrences against, first-seen date. **Never wire this to the UI or to the prompts.** It exists
solely so the harness can display "the agent moved at 4 observations here and 19 there" as a
fact rather than an impression.

---

## 8. Suggestion Loop

Beliefs reach the UI **only** through suggestions. Nothing is applied silently in Round 2.

### 8.1 Trigger

A suggestion may be generated when a belief is at `working theory` or above and has not been
suggested in the last 7 days. Cap at 2 suggestions per session, surfaced at natural boundaries
(page entry, post-idle) — never mid-interaction.

### 8.2 Presentation

```
Want me to keep these collapsed by default?
You've been closing them at the start of nearly every session for about three weeks.

[ Yes ]  [ No ]  [ Not quite — ]
```

- The rationale line is written by the agent, drawn from the belief's "What I'm leaning on."
- **"Not quite" opens a free-text field.** This is the highest-bandwidth channel in the system.
  A user typing *"I collapse them because I already reviewed those, not because I want them
  shut"* teaches the agent something no volume of click data would.
- Free text is passed **verbatim** into the next tier-1 prompt, clearly marked as the user
  speaking directly. Do not summarize it, do not preprocess it.

### 8.3 Probes

The agent may mark a suggestion as a probe — issued because it is uncertain, not because it is
confident. Probes are allowed at `tentative` conviction. They should be phrased as questions:

```
Quick question — when you open the top contracts first, are you going by margin,
or just by contract size?

[ Margin ]  [ Size ]  [ Something else — ]
```

Probes count against the per-session cap. Log them distinctly in the harness; the ratio of
probes to assertions over time is an interesting measurement in its own right.

### 8.4 Responses as interactions

Every response is logged as an `Interaction` with `suggestionId`, `suggestionResponse`, and
`amendText`. Accepted suggestions apply immediately and the resulting state carries
`causation: "agent-applied"`.

---

## 9. Storage Layout

```
data/
  glossary.json
  proposed-dimensions.json
  interactions/{userId}/{sessionId}.jsonl
  profiles/{userId}/
    control.md
    control.v{n}.md
    application.md
    application.v{n}.md
    universal.md
  runs/{runId}.json          # one per agent invocation — see §10
```

Flat files on disk are correct for Round 2. They are diffable, greppable, and readable without
tooling, which matters more here than query performance.

---

## 10. Observability Harness

**This is the primary deliverable.** If the framework works and the harness doesn't, we learn
nothing.

### 10.1 Run record

Every agent invocation writes `runs/{runId}.json`:

```ts
interface AgentRun {
  runId: string;
  ts: string;
  tier: 1 | 2 | 3;
  userId: string;
  sessionId?: string;
  trigger: "interaction-count" | "idle" | "session-end" | "weekly" | "manual-replay";

  promptVersion: string;     // hash of the system prompt
  contextHash: string;       // hash of the assembled surface tree
  glossaryVersion: string;

  inputInteractionIds: string[];
  profileVersionIn: number;

  rawRequest: string;        // full assembled prompt
  rawResponse: string;       // full agent output, pre-validation

  validationResult: "ok" | "retried" | "rejected";
  profileVersionOut?: number;
  profileDiff?: string;      // unified diff

  suggestionsGenerated: Array<{ id, dimension, isProbe, text }>;
  latencyMs: number;
  tokenCounts: { input: number; output: number };
}
```

### 10.2 Replay view

A UI (can be crude — a separate dev-only route is fine) showing per session:

1. Interaction stream, with causation and reversals visibly marked
2. The exact assembled context sent
3. The agent's raw output
4. The profile diff produced
5. Suggestions made and how the user responded
6. Shadow counter values at the moment of each revision

### 10.3 Replay-with-modified-prompt

Given a stored session's interactions and profile-version-in, re-run any tier against a modified
prompt and diff the resulting profile against what originally happened.

This is what turns prompt iteration from guesswork into something you can see. It should be a
single command:

```
npm run replay -- --session s_8841 --tier 1 --prompt ./prompts/tier1.v7.md
```

### 10.4 Synthetic personas

Scripted interaction streams that exercise known patterns, for testing without waiting weeks:

- **chart-preferring margin hawk** — consistent, should converge fast
- **long-habit reverser** — 200 sessions of one behavior, then 20 of the opposite. Measures
  reversal latency directly. Run this repeatedly; the variance is a finding.
- **confounded selector** — margin and size perfectly correlated for 30 sessions, then
  decorrelated. Does the agent notice, and does it say so before it's forced to?
- **contradictory** — genuinely inconsistent behavior. Does the agent invent a false pattern or
  admit uncertainty?

These are not pass/fail tests. They are instruments. Log what happens and look at it.

---

## 11. Build Order

1. Surface tree, `useSurface`, context assembly, run records
2. Interaction schema, `AdaptiveDisclosure` / `AdaptiveMetric` wrappers, choice-set capture
3. Profile document format, validation, versioning
4. Tier 1 agent, end to end, against the existing contracts page
5. Replay view
6. Suggestion loop with free-text amend
7. Tier 2 agent + proposed dimensions
8. Synthetic personas, replay-with-modified-prompt
9. Tier 3 scaffold

Steps 1–5 are the minimum that produces observations worth reading.

---

## 12. Open Questions

Carried forward deliberately — the implementation should make these answerable, not resolve them.

- Does stated conviction correlate with actual revision resistance, or is it decorative?
- Does the tier-2 agent produce genuine abstraction, or paraphrase tier 1?
- Do agents observing different users invent overlapping dimension vocabulary? (This is the
  ontology-governance question, answered empirically.)
- How much does `purpose` prose quality matter? Worth an A/B via replay: same sessions, degraded
  purpose strings.
- Is dwell time signal or noise? Captured but unused in prompts for Round 2; revisit with data.
- Does the free-text amend channel carry more information per token than everything else
  combined? Suspected yes.
