import { useCallback, useEffect, useRef, useState } from 'react'
import {
  adviseCollaboration,
  getCollaborationProfile,
  getContract,
  listContracts,
  submitCollaborationObservations,
  type CollaborationAdviseResponse,
  type ContractDetail,
  type ContractListItem,
} from '../api/client'
import {
  assembleSelectContractContext,
  assembleSelectContractObservations,
} from '../collaboration/assembleSelectContractContext'
import { createAppTendencyBundle } from '../collaboration/appDefaults'
import {
  SELECT_CONTRACT_SCREEN_ID,
  type CollaborationSuggestion,
  type CollaborationTendencyBundle,
} from '../collaboration/types'
import { useScreenObservations } from '../collaboration/useScreenObservations'
import './ContractsPage.css'

const PROFILE_REFRESH_DELAY_MS = 750

/** Browser forward / MouseX2 side button (not middle-click). */
const MOUSE_FORWARD_BUTTON = 4

type ContractsPageProps = {
  userId: string
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

type SignalsViewMode = 'values' | 'graph'

type RelativeSignal = {
  signalId: string
  key: string
  label: string
  display: string
  numeric: number | null
}

type SignalScale = {
  min: number
  max: number
}

function deliveryRiskRank(code: string): number {
  switch (code) {
    case 'Low':
      return 1
    case 'Medium':
      return 2
    case 'High':
      return 3
    default:
      return 0
  }
}

function strategicValueRank(code: string): number {
  switch (code) {
    case 'Low':
      return 1
    case 'Medium':
      return 2
    case 'High':
      return 3
    case 'VeryHigh':
      return 4
    default:
      return 0
  }
}

/** Resolve preferredLayout.expandBySignal to a numeric on ContractListItem. */
function contractSignalNumeric(
  item: ContractListItem,
  expandBySignal: string,
): number | null {
  const key = expandBySignal.trim().toLowerCase()
  if (
    key === 'margin' ||
    key === 'estimatedmarginpercent' ||
    key.includes('margin')
  ) {
    return item.estimatedMarginPercent
  }
  if (key === 'profit' || key === 'estimatedprofit' || key.includes('profit')) {
    return item.estimatedProfit
  }
  if (
    key === 'value' ||
    key === 'estimatedcontractvalue' ||
    (key.includes('value') && !key.includes('values'))
  ) {
    return item.estimatedContractValue
  }
  if (key.includes('win')) {
    return item.winProbabilityPercent
  }
  return null
}

/** Highest-ranked contract ids by preferred commercial signal (ties keep list order). */
function pickTopContractIdsBySignal(
  contracts: ContractListItem[],
  expandBySignal: string,
  count: number,
): string[] {
  if (count <= 0 || contracts.length === 0) {
    return []
  }

  return [...contracts]
    .map((item, index) => ({
      id: item.id,
      index,
      value: contractSignalNumeric(item, expandBySignal),
    }))
    .filter((row) => row.value != null)
    .sort((a, b) => {
      const delta = (b.value as number) - (a.value as number)
      return delta !== 0 ? delta : a.index - b.index
    })
    .slice(0, count)
    .map((row) => row.id)
}

function resolveBootstrapExpandIds(
  contracts: ContractListItem[],
  layout: CollaborationAdviseResponse['preferredLayout'] | null | undefined,
): string[] {
  if (!layout) {
    return []
  }

  const topCount =
    typeof layout.expandTopCount === 'number' && layout.expandTopCount > 0
      ? layout.expandTopCount
      : null
  const bySignal = layout.expandBySignal?.trim()

  if (topCount != null && bySignal) {
    return pickTopContractIdsBySignal(contracts, bySignal, topCount)
  }

  if (layout.expandAll) {
    return contracts.map((item) => item.id)
  }

  return []
}

function contractRelativeSignals(item: ContractListItem): RelativeSignal[] {
  return [
    {
      signalId: 'estimatedContractValue',
      key: 'Value',
      label: 'Value',
      display: formatMoney(item.estimatedContractValue),
      numeric: item.estimatedContractValue,
    },
    {
      signalId: 'estimatedProfit',
      key: 'Profit',
      label: 'Profit',
      display: formatMoney(item.estimatedProfit),
      numeric: item.estimatedProfit,
    },
    {
      signalId: 'estimatedMarginPercent',
      key: 'Margin',
      label: 'Margin',
      display: formatPercent(item.estimatedMarginPercent),
      numeric: item.estimatedMarginPercent,
    },
    {
      signalId: 'winProbabilityPercent',
      key: 'Win prob.',
      label: 'Win prob.',
      display: formatPercent(item.winProbabilityPercent),
      numeric: item.winProbabilityPercent,
    },
    {
      signalId: 'deliveryRisk',
      key: 'Delivery risk',
      label: 'Delivery risk',
      display: item.deliveryRiskName,
      numeric: deliveryRiskRank(item.deliveryRisk),
    },
    {
      signalId: 'durationWeeks',
      key: 'Duration',
      label: 'Duration',
      display: item.durationWeeks != null ? `${item.durationWeeks} wk` : '—',
      numeric: item.durationWeeks,
    },
    {
      signalId: 'strategicValue',
      key: 'Strategic',
      label: 'Strategic',
      display: item.strategicValueName,
      numeric: strategicValueRank(item.strategicValue),
    },
  ]
}

function buildSignalScales(contracts: ContractListItem[]): Record<string, SignalScale> {
  const scales: Record<string, SignalScale> = {}
  for (const item of contracts) {
    for (const signal of contractRelativeSignals(item)) {
      if (signal.numeric == null) {
        continue
      }
      const existing = scales[signal.key]
      if (!existing) {
        scales[signal.key] = { min: signal.numeric, max: signal.numeric }
      } else {
        existing.min = Math.min(existing.min, signal.numeric)
        existing.max = Math.max(existing.max, signal.numeric)
      }
    }
  }
  return scales
}

function relativeBarPercent(value: number | null, scale: SignalScale | undefined): number {
  if (value == null || !scale) {
    return 0
  }
  if (scale.max === scale.min) {
    return 100
  }
  return ((value - scale.min) / (scale.max - scale.min)) * 100
}

export function ContractsPage({ userId, onSelect, onError }: ContractsPageProps) {
  const [contracts, setContracts] = useState<ContractListItem[]>([])
  const [loading, setLoading] = useState(true)
  const [expandedIds, setExpandedIds] = useState<ReadonlySet<string>>(() => new Set())
  const [detailsById, setDetailsById] = useState<Record<string, ContractDetail>>({})
  const [detailLoadingIds, setDetailLoadingIds] = useState<ReadonlySet<string>>(
    () => new Set(),
  )
  const [tendencies, setTendencies] = useState<CollaborationTendencyBundle>(
    createAppTendencyBundle(),
  )
  const [advising, setAdvising] = useState(false)
  const [lastAdvise, setLastAdvise] = useState<CollaborationAdviseResponse | null>(null)
  const [debugOpen, setDebugOpen] = useState(false)
  const [signalsView, setSignalsView] = useState<SignalsViewMode>('values')

  const { emit, emitSignalFocus, emitSignalActivate, drain, peek } = useScreenObservations(
    SELECT_CONTRACT_SCREEN_ID,
  )

  const primarySuggestion = lastAdvise?.suggestions[0] ?? null
  const suggestionSatisfied = (() => {
    if (!primarySuggestion || advising) {
      return false
    }
    if (primarySuggestion.kind === 'set-view') {
      const target = primarySuggestion.payload?.signalsDisplay
      return target != null && target === signalsView
    }
    if (
      primarySuggestion.kind === 'expand' ||
      primarySuggestion.id === 'expand-first' ||
      primarySuggestion.id.startsWith('expand-')
    ) {
      return (
        primarySuggestion.targetControlId != null &&
        expandedIds.has(primarySuggestion.targetControlId)
      )
    }
    return false
  })()
  const forwardActionReady = Boolean(primarySuggestion && !advising && !suggestionSatisfied)
  const applySuggestionRef = useRef<() => void>(() => {})

  const refreshProfileSoon = useCallback(() => {
    window.setTimeout(() => {
      void getCollaborationProfile()
        .then((response) => setTendencies(response.tendencies))
        .catch(() => {
          /* demo panel refresh is best-effort */
        })
    }, PROFILE_REFRESH_DELAY_MS)
  }, [])

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      setLoading(true)
      onError(null)
      try {
        const [items, profileResponse] = await Promise.all([
          listContracts(),
          getCollaborationProfile().catch(() => null),
        ])
        if (cancelled) {
          return
        }

        const profile = profileResponse?.tendencies ?? createAppTendencyBundle()
        setContracts(items)
        setTendencies(profile)
        setExpandedIds(new Set())
        setDetailsById({})
        setSignalsView('values')
        setLoading(false)

        if (items.length === 0) {
          return
        }

        setAdvising(true)
        try {
          const response = await adviseCollaboration(
            assembleSelectContractContext({
              contracts: items,
              expandedIds: new Set(),
              detailsById: {},
              events: peek(),
              signalsDisplay: 'values',
            }),
          )
          if (cancelled) {
            return
          }

          setLastAdvise(response)

          const layout = response.preferredLayout
          const nextSignals: SignalsViewMode =
            layout?.signalsDisplay === 'graph' || layout?.signalsDisplay === 'values'
              ? layout.signalsDisplay
              : 'values'
          setSignalsView(nextSignals)

          const bootstrapIds = resolveBootstrapExpandIds(items, layout)
          if (bootstrapIds.length > 0) {
            const bootstrapExpandedIds = new Set(bootstrapIds)
            setExpandedIds(bootstrapExpandedIds)
            setDetailLoadingIds(new Set(bootstrapExpandedIds))
            const loaded = await Promise.all(
              [...bootstrapExpandedIds].map(async (contractId) => {
                try {
                  const detail = await getContract(contractId)
                  return [contractId, detail] as const
                } catch {
                  return null
                }
              }),
            )
            if (cancelled) {
              return
            }

            const nextDetailsById = Object.fromEntries(
              loaded.filter((entry): entry is readonly [string, ContractDetail] => entry != null),
            )
            setDetailsById(nextDetailsById)
            setDetailLoadingIds(new Set())
            if (Object.keys(nextDetailsById).length < bootstrapExpandedIds.size) {
              setExpandedIds(new Set(Object.keys(nextDetailsById)))
            }
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
        expandedIds,
        detailsById,
        events,
        signalsDisplay: signalsView,
      })
      const response = await adviseCollaboration(request)
      setLastAdvise(response)
      if (options?.openDebug) {
        setDebugOpen(true)
      }
      return response
    },
    [contracts, detailsById, expandedIds, signalsView],
  )

  const flushObservationsAndAdvise = useCallback(
    async (options?: {
      openDebug?: boolean
      nextExpandedIds?: ReadonlySet<string>
      nextDetailsById?: Record<string, ContractDetail>
      nextSignalsDisplay?: SignalsViewMode
    }) => {
      const events = drain()
      const response = await submitCollaborationObservations(
        assembleSelectContractObservations({
          userId,
          contracts,
          expandedIds: options?.nextExpandedIds ?? expandedIds,
          detailsById: options?.nextDetailsById ?? detailsById,
          events,
          signalsDisplay: options?.nextSignalsDisplay ?? signalsView,
        }),
      )
      setLastAdvise({
        promptPreview: response.promptPreview,
        suggestions: response.suggestions,
        preferredLayout: response.preferredLayout,
      })
      if (options?.openDebug) {
        setDebugOpen(true)
      }
      refreshProfileSoon()
      return response
    },
    [contracts, detailsById, drain, expandedIds, refreshProfileSoon, signalsView, userId],
  )

  async function expandContract(contractId: string) {
    if (expandedIds.has(contractId)) {
      return
    }

    const item = contracts.find((c) => c.id === contractId)
    const label = item ? `${item.code} ${item.title}` : contractId
    const nextExpandedIds = new Set(expandedIds)
    nextExpandedIds.add(contractId)

    emit('control.expand', {
      controlId: contractId,
      label,
      meta: {
        meaning: 'show-extended-detail',
        fromDetailLevel: 'summary',
        toDetailLevel: 'extended',
        signalsDisplay: signalsView,
      },
    })
    setExpandedIds(nextExpandedIds)

    // Exploration only — buffer the expand; flush to the agent on a later decision
    // (select or view.change).
    if (detailsById[contractId]) {
      return
    }

    setDetailLoadingIds((current) => {
      const next = new Set(current)
      next.add(contractId)
      return next
    })
    onError(null)
    try {
      const detail = await getContract(contractId)
      setDetailsById((current) => ({ ...current, [contractId]: detail }))
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Failed to load contract details')
      setExpandedIds((current) => {
        const next = new Set(current)
        next.delete(contractId)
        return next
      })
    } finally {
      setDetailLoadingIds((current) => {
        const next = new Set(current)
        next.delete(contractId)
        return next
      })
    }
  }

  async function toggleExpand(contractId: string) {
    if (expandedIds.has(contractId)) {
      const item = contracts.find((c) => c.id === contractId)
      const label = item ? `${item.code} ${item.title}` : contractId
      const nextExpandedIds = new Set(expandedIds)
      nextExpandedIds.delete(contractId)

      emit('control.collapse', {
        controlId: contractId,
        label,
        meta: {
          meaning: 'hide-extended-detail',
          fromDetailLevel: 'extended',
          toDetailLevel: 'summary',
          signalsDisplay: signalsView,
        },
      })
      setExpandedIds(nextExpandedIds)
      return
    }

    await expandContract(contractId)
  }

  async function handleSelect(contractId: string) {
    const item = contracts.find((c) => c.id === contractId)
    const label = item ? `${item.code} ${item.title}` : contractId
    emit('control.select', {
      controlId: contractId,
      label,
      meta: {
        meaning: 'chose-engagement-to-staff',
        detailLevel: expandedIds.has(contractId) ? 'extended' : 'summary',
        signalsDisplay: signalsView,
      },
    })

    setAdvising(true)
    onError(null)
    try {
      await flushObservationsAndAdvise()
      onSelect(contractId)
    } catch (err) {
      onError(
        err instanceof Error
          ? err.message
          : 'Failed to record collaboration observations before selecting',
      )
    } finally {
      setAdvising(false)
    }
  }

  async function setSignalsDisplay(next: SignalsViewMode) {
    if (next === signalsView) {
      return
    }

    emit('view.change', {
      label: 'signals-display',
      meta: {
        preferenceAxis: 'signalsDisplay',
        from: signalsView,
        to: next,
        meaning:
          next === 'graph'
            ? 'switched-to-relative-graph-signals'
            : 'switched-to-numeric-signal-values',
      },
    })
    setSignalsView(next)

    setAdvising(true)
    onError(null)
    try {
      await flushObservationsAndAdvise({ nextSignalsDisplay: next })
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Failed to refresh AI suggestion')
    } finally {
      setAdvising(false)
    }
  }

  async function applySuggestion(suggestion: CollaborationSuggestion) {
    if (advising) {
      return
    }

    if (suggestion.kind === 'set-view') {
      const next = suggestion.payload?.signalsDisplay
      if (next === 'values' || next === 'graph') {
        await setSignalsDisplay(next)
      }
      return
    }

    if (!suggestion.targetControlId) {
      return
    }

    if (suggestion.kind === 'select' || suggestion.id === 'navigate-selected') {
      await handleSelect(suggestion.targetControlId)
      return
    }

    if (suggestion.kind === 'collapse') {
      if (expandedIds.has(suggestion.targetControlId)) {
        await toggleExpand(suggestion.targetControlId)
      }
      return
    }

    // expand (default)
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
    ? primarySuggestion.kind === 'set-view'
      ? primarySuggestion.payload?.signalsDisplay === 'graph'
        ? 'Show graphs'
        : 'Show values'
      : primarySuggestion.kind === 'select'
        ? 'Select contract'
        : primarySuggestion.kind === 'collapse'
          ? 'Collapse details'
          : 'Expand details'
    : null
  const signalScales = buildSignalScales(contracts)

  return (
    <section className="contracts-page">
      <header className="contracts-page-header">
        <h1>Select a contract</h1>
        <p className="muted">
          Expand a card to compare capacity and scope, then select the engagement to build teams.
        </p>
        <div className="contracts-page-tools">
          <div className="contracts-signals-view-toggle" role="group" aria-label="Signal display">
            <button
              type="button"
              className={signalsView === 'values' ? undefined : 'secondary'}
              aria-pressed={signalsView === 'values'}
              disabled={advising || loading}
              onClick={() => void setSignalsDisplay('values')}
            >
              Values
            </button>
            <button
              type="button"
              className={signalsView === 'graph' ? undefined : 'secondary'}
              aria-pressed={signalsView === 'graph'}
              disabled={advising || loading}
              onClick={() => void setSignalsDisplay('graph')}
            >
              Graph
            </button>
          </div>
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
                  disabled={
                    primarySuggestion.kind !== 'set-view' && !primarySuggestion.targetControlId
                  }
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
            const expanded = expandedIds.has(item.id)
            const detail = detailsById[item.id]
            const detailLoading = detailLoadingIds.has(item.id)
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

                  {signalsView === 'values' ? (
                    <dl className="contract-select-signals">
                      {contractRelativeSignals(item).map((signal) => (
                        <div
                          key={signal.signalId}
                          tabIndex={0}
                          onMouseEnter={() =>
                            emitSignalFocus(item.id, signal.signalId, signal.label, {
                              controlLabel: cardLabel,
                              signalsDisplay: signalsView,
                            })
                          }
                          onFocus={() =>
                            emitSignalFocus(item.id, signal.signalId, signal.label, {
                              controlLabel: cardLabel,
                              signalsDisplay: signalsView,
                            })
                          }
                          onDoubleClick={() =>
                            emitSignalActivate(item.id, signal.signalId, signal.label, {
                              controlLabel: cardLabel,
                              signalsDisplay: signalsView,
                            })
                          }
                        >
                          <dt>{signal.label}</dt>
                          <dd>{signal.display}</dd>
                        </div>
                      ))}
                    </dl>
                  ) : (
                    <div
                      className="contract-select-signal-bars"
                      aria-label="Relative signal comparison"
                    >
                      {contractRelativeSignals(item).map((signal) => {
                        const percent = relativeBarPercent(
                          signal.numeric,
                          signalScales[signal.key],
                        )
                        return (
                          <div
                            key={signal.signalId}
                            className="contract-select-signal-bar-row"
                            tabIndex={0}
                            onMouseEnter={() =>
                              emitSignalFocus(item.id, signal.signalId, signal.label, {
                                controlLabel: cardLabel,
                                signalsDisplay: signalsView,
                              })
                            }
                            onFocus={() =>
                              emitSignalFocus(item.id, signal.signalId, signal.label, {
                                controlLabel: cardLabel,
                                signalsDisplay: signalsView,
                              })
                            }
                            onDoubleClick={() =>
                              emitSignalActivate(item.id, signal.signalId, signal.label, {
                                controlLabel: cardLabel,
                                signalsDisplay: signalsView,
                              })
                            }
                          >
                            <span className="contract-select-signal-bar-label">
                              {signal.label}
                            </span>
                            <span
                              className="contract-select-signal-bar-track"
                              aria-hidden="true"
                            >
                              <span
                                className="contract-select-signal-bar-fill"
                                style={{ width: `${percent}%` }}
                              />
                            </span>
                            <span className="contract-select-signal-bar-value">
                              {signal.display}
                            </span>
                          </div>
                        )
                      })}
                    </div>
                  )}

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
            <h3>User profile (demo)</h3>
            <pre>
              {tendencies.userOverride ?? tendencies.appDefaults}
              {'\n\n'}
              source: {tendencies.source}
              {tendencies.updatedAt ? `\nupdated: ${tendencies.updatedAt}` : ''}
            </pre>
            {tendencies.recentTurnDigests && tendencies.recentTurnDigests.length > 0 && (
              <>
                <h3>Recent decision digests</h3>
                <pre>{tendencies.recentTurnDigests.map((d, i) => `${i + 1}) ${d}`).join('\n')}</pre>
              </>
            )}
            {lastAdvise.preferredLayout && (
              <>
                <h3>Preferred layout (from advisor)</h3>
                <pre>
                  {JSON.stringify(lastAdvise.preferredLayout, null, 2)}
                </pre>
              </>
            )}
            <h3>Prompt preview</h3>
            <pre>{lastAdvise.promptPreview}</pre>
            {lastAdvise.suggestions.length > 0 && (
              <>
                <h3>Suggestions</h3>
                <ul className="contracts-collab-suggestions">
                  {lastAdvise.suggestions.map((suggestion) => (
                    <li key={suggestion.id}>
                      <span>
                        [{suggestion.kind}] {suggestion.label}
                        {suggestion.payload
                          ? ` · ${Object.entries(suggestion.payload)
                              .map(([k, v]) => `${k}=${v}`)
                              .join(', ')}`
                          : ''}
                      </span>
                      <button
                        type="button"
                        className="secondary"
                        disabled={
                          advising ||
                          (suggestion.kind !== 'set-view' && !suggestion.targetControlId)
                        }
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
