import { useEffect, useState } from 'react'
import { listContracts, type ContractListItem } from '../api/client'
import './ContractsPage.css'

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

export function ContractsPage({ onSelect, onError }: ContractsPageProps) {
  const [contracts, setContracts] = useState<ContractListItem[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      setLoading(true)
      onError(null)
      try {
        const items = await listContracts()
        if (!cancelled) {
          setContracts(items)
        }
      } catch (err) {
        if (!cancelled) {
          onError(err instanceof Error ? err.message : 'Failed to load contracts')
        }
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [onError])

  return (
    <section className="contracts-page">
      <header className="contracts-page-header">
        <h1>Select a contract</h1>
        <p className="muted">
          Choose an engagement to open its brief and build teams against that scope.
        </p>
      </header>

      {loading && <div className="contracts-empty">Loading contracts…</div>}

      {!loading && contracts.length === 0 && (
        <div className="contracts-empty">No contracts are available yet.</div>
      )}

      {!loading && contracts.length > 0 && (
        <ul className="contracts-grid">
          {contracts.map((item) => {
            const target = formatDate(item.targetDeliveryDate)
            const timeline = [
              item.durationWeeks != null ? `${item.durationWeeks} weeks` : null,
              target ? `target ${target}` : null,
            ]
              .filter(Boolean)
              .join(' · ')

            return (
              <li key={item.id}>
                <button
                  type="button"
                  className="contract-select-card"
                  onClick={() => onSelect(item.id)}
                >
                  <div className="contract-select-kicker">
                    {item.code} · {item.clientName}
                  </div>
                  <h2>{item.title}</h2>
                  <p>{item.outcomeSummary}</p>
                  <div className="contract-select-meta">
                    <span>{item.engagementTypeName}</span>
                    <span>{item.workModeName}</span>
                    {timeline && <span>{timeline}</span>}
                    <span>
                      {item.teamCount} {item.teamCount === 1 ? 'team' : 'teams'}
                    </span>
                  </div>
                </button>
              </li>
            )
          })}
        </ul>
      )}
    </section>
  )
}
