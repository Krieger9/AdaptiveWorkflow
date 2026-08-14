import { useCallback, useEffect, useState } from 'react'
import {
  getCollaborationRun,
  getCollaborationSessionInteractions,
  listCollaborationRuns,
  listCollaborationSessions,
  replayCollaborationSession,
  type AgentRunRecord,
  type AgentRunSummary,
  type Interaction,
} from '../api/client'
import './ReplayPage.css'

type ReplayPageProps = {
  onError: (message: string | null) => void
}

/**
 * Dev-only observability harness: the interaction stream (causation/reversals
 * marked), assembled context, raw agent output, profile diff, approvals, and
 * replay-with-modified-prompt. "Here's the system thinking."
 */
export function ReplayPage({ onError }: ReplayPageProps) {
  const [sessions, setSessions] = useState<string[]>([])
  const [selectedSession, setSelectedSession] = useState<string | null>(null)
  const [interactions, setInteractions] = useState<Interaction[]>([])
  const [runs, setRuns] = useState<AgentRunSummary[]>([])
  const [selectedRun, setSelectedRun] = useState<AgentRunRecord | null>(null)
  const [promptOverride, setPromptOverride] = useState('')
  const [replaying, setReplaying] = useState(false)
  const [replayResult, setReplayResult] = useState<AgentRunRecord | null>(null)

  const refresh = useCallback(async () => {
    onError(null)
    try {
      const [sessionIds, runSummaries] = await Promise.all([
        listCollaborationSessions(),
        listCollaborationRuns(),
      ])
      setSessions(sessionIds)
      setRuns(runSummaries)
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Failed to load replay data')
    }
  }, [onError])

  useEffect(() => {
    void refresh()
  }, [refresh])

  async function openSession(sessionId: string) {
    setSelectedSession(sessionId)
    setReplayResult(null)
    onError(null)
    try {
      setInteractions(await getCollaborationSessionInteractions(sessionId))
    } catch (err) {
      setInteractions([])
      onError(err instanceof Error ? err.message : 'Failed to load session interactions')
    }
  }

  async function openRun(runId: string) {
    onError(null)
    try {
      setSelectedRun(await getCollaborationRun(runId))
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Failed to load run record')
    }
  }

  async function replay() {
    if (!selectedSession || replaying) {
      return
    }
    setReplaying(true)
    onError(null)
    try {
      const record = await replayCollaborationSession({
        sessionId: selectedSession,
        promptOverride: promptOverride.trim() ? promptOverride : null,
      })
      setReplayResult(record)
      await refresh()
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Replay failed')
    } finally {
      setReplaying(false)
    }
  }

  function causationBadge(interaction: Interaction) {
    const causation = interaction.causation ?? 'user'
    return (
      <span className={`replay-causation replay-causation-${causation}`}>
        {causation}
        {interaction.reversal ? ' · REVERSAL' : ''}
      </span>
    )
  }

  return (
    <section className="replay-page">
      <header className="replay-header">
        <h1>Collaboration replay (dev only)</h1>
        <button type="button" className="secondary" onClick={() => void refresh()}>
          Refresh
        </button>
      </header>

      <div className="replay-columns">
        <div className="replay-column">
          <h2>Sessions</h2>
          {sessions.length === 0 && <p className="muted">No recorded sessions yet.</p>}
          <ul className="replay-list">
            {sessions.map((sessionId) => (
              <li key={sessionId}>
                <button
                  type="button"
                  className={sessionId === selectedSession ? 'linkish active' : 'linkish'}
                  onClick={() => void openSession(sessionId)}
                >
                  {sessionId}
                </button>
              </li>
            ))}
          </ul>

          <h2>Agent runs</h2>
          {runs.length === 0 && <p className="muted">No run records yet.</p>}
          <ul className="replay-list">
            {runs.map((run) => (
              <li key={run.runId}>
                <button
                  type="button"
                  className={run.runId === selectedRun?.runId ? 'linkish active' : 'linkish'}
                  onClick={() => void openRun(run.runId)}
                >
                  {run.ts} · {run.agent} ({run.source}) · {run.trigger}
                  {run.validationResult ? ` · ${run.validationResult}` : ''}
                </button>
              </li>
            ))}
          </ul>
        </div>

        <div className="replay-column replay-detail">
          {selectedSession && (
            <>
              <h2>Interaction stream — {selectedSession}</h2>
              <ol className="replay-interactions">
                {interactions.map((interaction) => (
                  <li key={interaction.id} className={interaction.reversal ? 'reversal' : undefined}>
                    <code>{interaction.seq}</code> {interaction.action}
                    {interaction.label ? ` · ${interaction.label}` : ''}
                    {causationBadge(interaction)}
                    <span className="muted"> {interaction.surfacePath.join(' › ')}</span>
                  </li>
                ))}
              </ol>

              <h3>Replay against a modified prompt</h3>
              <textarea
                className="replay-prompt"
                rows={6}
                placeholder="Optional prompt override — leave blank to replay with the current prompt."
                value={promptOverride}
                onChange={(event) => setPromptOverride(event.target.value)}
              />
              <button type="button" disabled={replaying} onClick={() => void replay()}>
                {replaying ? 'Replaying…' : 'Replay session'}
              </button>
              {replayResult && <RunRecordView title="Replay result" record={replayResult} />}
            </>
          )}

          {selectedRun && <RunRecordView title={`Run ${selectedRun.runId}`} record={selectedRun} />}

          {!selectedSession && !selectedRun && (
            <p className="muted">Pick a session or run record to inspect.</p>
          )}
        </div>
      </div>
    </section>
  )
}

function RunRecordView({ title, record }: { title: string; record: AgentRunRecord }) {
  return (
    <details className="replay-run" open>
      <summary>{title}</summary>
      <dl className="replay-meta">
        <dt>Agent / source</dt>
        <dd>{record.agent} · {record.source} · tier {record.tier}</dd>
        <dt>Trigger</dt>
        <dd>{record.trigger}</dd>
        <dt>Prompt version</dt>
        <dd>{record.promptVersion}</dd>
        <dt>Context hash</dt>
        <dd>{record.contextHash ?? '—'}</dd>
        <dt>Glossary version</dt>
        <dd>{record.glossaryVersion}</dd>
        <dt>Profile version</dt>
        <dd>
          {record.profileVersionIn ?? '—'} → {record.profileVersionOut ?? '—'}
        </dd>
        <dt>Validation</dt>
        <dd>{record.validationResult ?? '—'}</dd>
        <dt>Latency</dt>
        <dd>{record.latencyMs} ms</dd>
        <dt>Input interactions</dt>
        <dd>{record.inputInteractionIds.length > 0 ? record.inputInteractionIds.join(', ') : '—'}</dd>
      </dl>

      {record.approvals && record.approvals.length > 0 && (
        <>
          <h4>Approval decisions</h4>
          <ul>
            {record.approvals.map((approval) => (
              <li key={approval.adaptationId}>
                {approval.approved ? '✔' : '✘'} {approval.adaptationKind} ·{' '}
                {approval.adaptationId} ({approval.policy})
                {approval.belief ? ` — belief: ${approval.belief}` : ''}
                {approval.rationale ? ` — ${approval.rationale}` : ''}
              </li>
            ))}
          </ul>
        </>
      )}

      <h4>Assembled context / raw request</h4>
      <pre>{record.rawRequest}</pre>

      <h4>Raw agent output</h4>
      <pre>{record.rawResponse ?? '—'}</pre>

      {record.profileDiff && (
        <>
          <h4>Profile diff</h4>
          <pre>{record.profileDiff}</pre>
        </>
      )}

      {record.shadowCounters && Object.keys(record.shadowCounters).length > 0 && (
        <>
          <h4>Shadow counters (never fed to prompts)</h4>
          <ul>
            {Object.entries(record.shadowCounters).map(([dimension, counter]) => (
              <li key={dimension}>
                {dimension}: for {counter.for} / against {counter.against}
                {counter.firstSeen ? ` (first seen ${counter.firstSeen})` : ''}
              </li>
            ))}
          </ul>
        </>
      )}

      {record.error && (
        <>
          <h4>Error</h4>
          <pre>{record.error}</pre>
        </>
      )}
    </details>
  )
}
