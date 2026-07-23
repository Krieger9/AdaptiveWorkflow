import { useCallback, useEffect, useRef, useState } from 'react'
import {
  adviseCollaboration,
  getCollaborationTendencies,
  getContract,
  listContracts,
  type CollaborationAdviseResponse,
  type ContractDetail,
  type ContractListItem,
} from '../api/client'
import { assembleSelectContractContext } from '../collaboration/assembleSelectContractContext'
import { createAppTendencyBundle } from '../collaboration/appDefaults'
import {
  SELECT_CONTRACT_SCREEN_ID,
  type CollaborationSuggestion,
  type CollaborationTendencyBundle,
} from '../collaboration/types'
import { useScreenObservations } from '../collaboration/useScreenObservations'
import './ContractsPage.css'

/** Browser forward / MouseX2 side button (not middle-click). */
const MOUSE_FORWARD_BUTTON = 4

type ContractsPageProps = {
  onSelect: (contractId: string) => void
  onError: (message: string | null) => void
}

function formatDate(value: string | null): string | null {
  if (!value) {
    return null
  }
  const date = new Date(`${value}T00:00:00`)
  if (Number.isNaN(date.getTime())) {
    return value
  }
  return date.toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

function formatMoney(value: number): string {
  if (Math.abs(value) >= 1_000_000) {
    const millions = value / 1_000_000
    return `$${millions.toFixed(millions >= 10 ? 0 : 1)}M`
  }
  if (Math.abs(value) >= 1_000) {
    return `$${Math.round(value / 1_000)}K`
  }
  return new Intl.NumberFormat(undefined, {
    style: 'currency',
    currency: 'USD',
    maximumFractionDigits: 0,
  }).format(value)
}

function formatPercent(value: number): string {
  return `${Number.isInteger(value) ? value.toFixed(0) : value.toFixed(1)}%`
}

function formatFte(value: number): string {
  return `${Number.isInteger(value) ? value.toFixed(0) : value.toFixed(1)} FTE`
}

export function ContractsPage({ onSelect, onError }: ContractsPageProps) {
  const [contracts, setContracts] = useState<ContractListItem[]>([])
  const [loading, setLoading] = useState(true)
  const [expandedId, setExpandedId] = useState<string | null>(null)
  const [detailsById, setDetailsById] = useState<Record<string, ContractDetail>>({})
  const [detailLoadingId, setDetailLoadingId] = useState<string | null>(null)
  const [tendencies, setTendencies] = useState<CollaborationTendencyBundle>(
    createAppTendencyBundle(),
  )
  const [advising, setAdvising] = useState(false)
  const [lastAdvise, setLastAdvise] = useState<CollaborationAdviseResponse | null>(null)
  const [debugOpen, setDebugOpen] = useState(false)

  const { emit, emitSignalFocus, drain, peek } = useScreenObservations(
    SELECT_CONTRACT_SCREEN_ID,
  )

  const primarySuggestion = lastAdvise?.suggestions[0] ?? null
  const suggestionSatisfied =
    primarySuggestion?.targetControlId != null &&
    (primarySuggestion.id === 'expand-first' || primarySuggestion.id.startsWith('expand-')) &&
    expandedId === primarySuggestion.targetControlId
  const forwardActionReady = Boolean(
    primarySuggestion?.targetControlId && !advising && !suggestionSatisfied,
  )
  const applySuggestionRef = useRef<() => void>(() => {})

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      setLoading(true)
      onError(null)
      try {
        const [items, tendencyResponse] = await Promise.all([
          listContracts(),
          getCollaborationTendencies().catch(() => null),
        ])
        if (cancelled) {
          return
        }

        const nextTendencies = tendencyResponse?.tendencies ?? createAppTendencyBundle()
        setContracts(items)
        setTendencies(nextTendencies)
        setLoading(false)

        if (items.length === 0) {
          return
        }

        setAdvising(true)
        try {
          const response = await adviseCollaboration(
            assembleSelectContractContext({
              contracts: items,
              expandedId: null,
              detailsById: {},
              events: peek(),
              tendencies: nextTendencies,
            }),
          )
          if (!cancelled) {
            setTendencies(response.updatedTendencies)
            setLastAdvise(response)
          }
        } catch (err) {
          if (!cancelled) {
            onError(err instanceof Error ? err.message : 'Failed to load AI suggestion')
          }
        } finally {
          if (!cancelled) {
            setAdvising(false)
          }
        }
      } catch (err) {
        if (!cancelled) {
          onError(err instanceof Error ? err.message : 'Failed to load contracts')
          setLoading(false)
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [onError, peek])

  const runAdvise = useCallback(
    async (
      events: ReturnType<typeof peek>,
      options?: { openDebug?: boolean },
    ) => {
      const request = assembleSelectContractContext({
        contracts,
        expandedId,
        detailsById,
        events,
        tendencies,
      })
      const response = await adviseCollaboration(request)
      setTendencies(response.updatedTendencies)
      setLastAdvise(response)
      if (options?.openDebug) {
        setDebugOpen(true)
      }
      return response
    },
    [contracts, detailsById, expandedId, tendencies],
  )

  async function expandContract(contractId: string) {
    if (expandedId === contractId) {
      return
    }

    const item = contracts.find((c) => c.id === contractId)
    const label = item ? `${item.code} ${item.title}` : contractId

    emit('control.expand', { controlId: contractId, label })
    setExpandedId(contractId)
    if (detailsById[contractId]) {
      return
    }

    setDetailLoadingId(contractId)
    onError(null)
    try {
      const detail = await getContract(contractId)
      setDetailsById((current) => ({ ...current, [contractId]: detail }))
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Failed to load contract details')
      setExpandedId(null)
    } finally {
      setDetailLoadingId(null)
    }
  }

  async function toggleExpand(contractId: string) {
    if (expandedId === contractId) {
      const item = contracts.find((c) => c.id === contractId)
      const label = item ? `${item.code} ${item.title}` : contractId
      emit('control.collapse', { controlId: contractId, label })
      setExpandedId(null)
      return
    }

    await expandContract(contractId)
  }

  async function handleSelect(contractId: string) {
    const item = contracts.find((c) => c.id === contractId)
    const label = item ? `${item.code} ${item.title}` : contractId
    emit('control.select', { controlId: contractId, label })

    setAdvising(true)
    onError(null)
    try {
      await runAdvise(drain(), { openDebug: false })
      onSelect(contractId)
    } catch (err) {
      onError(
        err instanceof Error
          ? err.message
          : 'Failed to record collaboration context before selecting',
      )
    } finally {
      setAdvising(false)
    }
  }

  async function applySuggestion(suggestion: CollaborationSuggestion) {
    if (advising || !suggestion.targetControlId) {
      return
    }

    if (suggestion.id === 'navigate-selected') {
      await handleSelect(suggestion.targetControlId)
      return
    }

    // expand-first and any future expand-* suggestions
    await expandContract(suggestion.targetControlId)
  }

  async function applyPrimarySuggestion() {
    if (!primarySuggestion || suggestionSatisfied) {
      return
    }
    await applySuggestion(primarySuggestion)
  }

  applySuggestionRef.current = () => {
    void applyPrimarySuggestion()
  }

  useEffect(() => {
    function onMouseDown(event: MouseEvent) {
      if (event.button !== MOUSE_FORWARD_BUTTON || !forwardActionReady) {
        return
      }
      // Stop browser history-forward navigation when we own this button.
      event.preventDefault()
    }

    function onMouseUp(event: MouseEvent) {
      if (event.button !== MOUSE_FORWARD_BUTTON || !forwardActionReady) {
        return
      }
      event.preventDefault()
      event.stopPropagation()
      applySuggestionRef.current()
    }

    function onAuxClick(event: MouseEvent) {
      if (event.button !== MOUSE_FORWARD_BUTTON || !forwardActionReady) {
        return
      }
      event.preventDefault()
      event.stopPropagation()
    }

    window.addEventListener('mousedown', onMouseDown, true)
    window.addEventListener('mouseup', onMouseUp, true)
    window.addEventListener('auxclick', onAuxClick, true)
    return () => {
      window.removeEventListener('mousedown', onMouseDown, true)
      window.removeEventListener('mouseup', onMouseUp, true)
      window.removeEventListener('auxclick', onAuxClick, true)
    }
  }, [forwardActionReady])

  async function handleInspectContext() {
    // Already have context from page load — just reveal the debug panel.
    if (lastAdvise) {
      setDebugOpen(true)
      return
    }

    setAdvising(true)
    onError(null)
    try {
      await runAdvise(peek(), { openDebug: true })
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Failed to inspect collaboration context')
    } finally {
      setAdvising(false)
    }
  }

  const suggestedContractId =
    primarySuggestion && !suggestionSatisfied ? primarySuggestion.targetControlId : null
  const suggestedContract = suggestedContractId
    ? contracts.find((item) => item.id === suggestedContractId)
    : null
  const forwardActionLabel = primarySuggestion
    ? primarySuggestion.id === 'navigate-selected'
      ? 'Select contract'
      : 'Expand details'
    : null


  return (
    <section className="contracts-page">
      <header className="contracts-page-header">
        <h1>Select a contract</h1>
        <p className="muted">
          Expand a card to compare capacity and scope, then select the engagement to build teams.
        </p>
        <div className="contracts-page-tools">
          <button
            type="button"
            className="secondary"
            disabled={advising || loading || (!lastAdvise && contracts.length === 0)}
            onClick={() => void handleInspectContext()}
          >
            Inspect context
          </button>
        </div>
      </header>

      {!loading && contracts.length > 0 && (
        <aside
          className={`contracts-ai-suggestion${forwardActionReady ? ' ready' : ''}${suggestionSatisfied ? ' satisfied' : ''}`}
          aria-live="polite"
        >
          {advising && !primarySuggestion ? (
            <div className="contracts-ai-suggestion-copy">
              <span className="contracts-ai-suggestion-kicker">AI suggestion</span>
              <p>Reading the screen and preparing advice…</p>
            </div>
          ) : primarySuggestion ? (
            <>
              <div className="contracts-ai-suggestion-copy">
                <span className="contracts-ai-suggestion-kicker">
                  {suggestionSatisfied ? 'Suggestion applied' : 'AI suggestion'}
                </span>
                <p>{primarySuggestion.label}</p>
                {forwardActionReady && (
                  <p className="contracts-ai-suggestion-binding">
                    <kbd>Forward</kbd>
                    <span aria-hidden="true">→</span>
                    <strong>{forwardActionLabel}</strong>
                    {suggestedContract && (
                      <span className="muted">
                        on {suggestedContract.code} · {suggestedContract.title}
                      </span>
                    )}
                  </p>
                )}
                {suggestionSatisfied && (
                  <p className="muted contracts-ai-suggestion-hint">
                    Target card is expanded. Inspect context to review the collaboration prompt.
                  </p>
                )}
              </div>
              {forwardActionReady && (
                <button
                  type="button"
                  disabled={!primarySuggestion.targetControlId}
                  onClick={() => void applyPrimarySuggestion()}
                >
                  Accept
                </button>
              )}
            </>
          ) : (
            <div className="contracts-ai-suggestion-copy">
              <span className="contracts-ai-suggestion-kicker">AI suggestion</span>
              <p className="muted">No suggestion available yet.</p>
            </div>
          )}
        </aside>
      )}

      {loading && <div className="contracts-empty">Loading contracts…</div>}

      {!loading && contracts.length === 0 && (
        <div className="contracts-empty">No contracts are available yet.</div>
      )}

      {!loading && contracts.length > 0 && (
        <ul className="contracts-grid">
          {contracts.map((item) => {
            const expanded = expandedId === item.id
            const detail = detailsById[item.id]
            const detailLoading = detailLoadingId === item.id
            const target = formatDate(item.targetDeliveryDate)
            const timeline = [
              item.durationWeeks != null ? `${item.durationWeeks} weeks` : null,
              target ? `target ${target}` : null,
            ]
              .filter(Boolean)
              .join(' · ')
            const mustHave = detail?.skills.filter((s) => s.priority === 'MustHave') ?? []
            const cardLabel = `${item.code} ${item.title}`

            return (
              <li key={item.id}>
                <article
                  className={[
                    'contract-select-card',
                    expanded ? 'expanded' : '',
                    item.id === suggestedContractId ? 'suggested' : '',
                  ]
                    .filter(Boolean)
                    .join(' ')}
                >
                  {item.id === suggestedContractId && (
                    <div className="contract-select-suggested-badge">
                      <kbd>Forward</kbd> {forwardActionLabel}
                    </div>
                  )}
                  <div className="contract-select-kicker">
                    {item.code} · {item.clientName}
                  </div>
                  <h2>{item.title}</h2>
                  <p>{item.outcomeSummary}</p>

                  <dl className="contract-select-signals">
                    {(
                      [
                        ['Value', formatMoney(item.estimatedContractValue), 'Value'],
                        ['Profit', formatMoney(item.estimatedProfit), 'Profit'],
                        ['Margin', formatPercent(item.estimatedMarginPercent), 'Margin'],
                        ['Win prob.', formatPercent(item.winProbabilityPercent), 'Win prob.'],
                        ['Delivery risk', item.deliveryRiskName, 'Delivery risk'],
                        ['Strategic', item.strategicValueName, 'Strategic'],
                      ] as const
                    ).map(([dt, dd, signal]) => (
                      <div
                        key={signal}
                        tabIndex={0}
                        onMouseEnter={() => emitSignalFocus(item.id, signal, cardLabel)}
                        onFocus={() => emitSignalFocus(item.id, signal, cardLabel)}
                      >
                        <dt>{dt}</dt>
                        <dd>{dd}</dd>
                      </div>
                    ))}
                  </dl>

                  <div className="contract-select-meta">
                    <span>{item.engagementTypeName}</span>
                    <span>{item.workModeName}</span>
                    {timeline && <span>{timeline}</span>}
                    <span>
                      {item.teamCount} {item.teamCount === 1 ? 'team' : 'teams'}
                    </span>
                  </div>

                  {expanded && (
                    <div className="contract-select-details">
                      {detailLoading && !detail && (
                        <p className="contract-select-details-loading">
                          Loading contract details…
                        </p>
                      )}

                      {detail && (
                        <>
                          <div>
                            <h3>Capacity</h3>
                            <ul>
                              <li>
                                <strong>Staffing</strong> — {formatFte(detail.staffingFte)}
                              </li>
                              {detail.specialistStaffingNeeded && (
                                <li>
                                  <strong>Specialists</strong> —{' '}
                                  {detail.specialistStaffingNeeded}
                                </li>
                              )}
                              {detail.durationWeeks != null && (
                                <li>
                                  <strong>Duration</strong> — {detail.durationWeeks} weeks
                                </li>
                              )}
                            </ul>
                          </div>
                          <div>
                            <h3>Scope</h3>
                            <p>{detail.scopeSummary}</p>
                          </div>
                          {mustHave.length > 0 && (
                            <div>
                              <h3>Must-have skills</h3>
                              <div className="contract-select-skill-chips">
                                {mustHave.map((skill) => (
                                  <span key={skill.name}>{skill.name}</span>
                                ))}
                              </div>
                            </div>
                          )}
                          {detail.constraints.length > 0 && (
                            <div>
                              <h3>Constraints</h3>
                              <ul>
                                {detail.constraints.map((constraint) => (
                                  <li key={constraint.code}>{constraint.name}</li>
                                ))}
                              </ul>
                            </div>
                          )}
                        </>
                      )}
                    </div>
                  )}

                  <div className="contract-select-actions">
                    <button
                      type="button"
                      className="secondary"
                      onClick={() => void toggleExpand(item.id)}
                      aria-expanded={expanded}
                      disabled={advising}
                    >
                      {expanded ? 'Collapse' : 'Expand details'}
                    </button>
                    <button
                      type="button"
                      onClick={() => void handleSelect(item.id)}
                      disabled={advising}
                    >
                      {advising ? 'Recording…' : 'Select contract'}
                    </button>
                  </div>
                </article>
              </li>
            )
          })}
        </ul>
      )}

      {lastAdvise && (
        <details
          className="contracts-collab-debug"
          open={debugOpen}
          onToggle={(event) => setDebugOpen(event.currentTarget.open)}
        >
          <summary>Collaboration context debug</summary>
          <div className="contracts-collab-debug-body">
            <h3>Active tendencies</h3>
            <pre>
              {tendencies.userOverride ?? tendencies.appDefaults}
              {'\n\n'}
              source: {tendencies.source}
              {tendencies.updatedAt ? `\nupdated: ${tendencies.updatedAt}` : ''}
            </pre>
            <h3>Prompt preview</h3>
            <pre>{lastAdvise.promptPreview}</pre>
            {lastAdvise.suggestions.length > 0 && (
              <>
                <h3>Stub suggestions</h3>
                <ul className="contracts-collab-suggestions">
                  {lastAdvise.suggestions.map((suggestion) => (
                    <li key={suggestion.id}>
                      <span>
                        [{suggestion.kind}] {suggestion.label}
                      </span>
                      <button
                        type="button"
                        className="secondary"
                        disabled={advising || !suggestion.targetControlId}
                        onClick={() => void applySuggestion(suggestion)}
                      >
                        Accept
                      </button>
                    </li>
                  ))}
                </ul>
              </>
            )}
          </div>
        </details>
      )}
    </section>
  )
}
