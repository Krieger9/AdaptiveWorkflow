import { useState } from 'react'
import type { ContractDetail } from '../api/client'
import './ContractBrief.css'

type ContractBriefProps = {
  contract: ContractDetail | null
  loading?: boolean
  onChangeContract?: () => void
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

export function ContractBrief({ contract, loading, onChangeContract }: ContractBriefProps) {
  const [expanded, setExpanded] = useState(false)

  if (loading && !contract) {
    return (
      <section className="contract-brief" aria-busy="true">
        <div className="contract-brief-empty">Loading contract brief…</div>
      </section>
    )
  }

  if (!contract) {
    return (
      <section className="contract-brief">
        <div className="contract-brief-empty">No contract brief available.</div>
      </section>
    )
  }

  const mustHave = contract.skills.filter((s) => s.priority === 'MustHave')
  const niceToHave = contract.skills.filter((s) => s.priority === 'NiceToHave')
  const targetLabel = formatDate(contract.targetDeliveryDate)
  const timelineLabel = [
    contract.durationWeeks != null ? `${contract.durationWeeks} weeks` : null,
    targetLabel ? `target ${targetLabel}` : null,
  ]
    .filter(Boolean)
    .join(' · ')

  return (
    <section className={`contract-brief${expanded ? ' expanded' : ''}`}>
      <div className="contract-brief-top">
        <div className="contract-brief-identity">
          <div className="contract-brief-kicker">
            Contract brief · {contract.code}
          </div>
          <h2>{contract.title}</h2>
          <p className="contract-brief-outcome">{contract.outcomeSummary}</p>
        </div>
        <div className="contract-brief-actions">
          {onChangeContract && (
            <button
              type="button"
              className="secondary"
              onClick={onChangeContract}
            >
              Change contract
            </button>
          )}
          <button
            type="button"
            className="secondary contract-brief-toggle"
            onClick={() => setExpanded((value) => !value)}
            aria-expanded={expanded}
          >
            {expanded ? 'Collapse' : 'Expand'}
          </button>
        </div>
      </div>

      <div className="contract-brief-meta">
        <span className="contract-chip">{contract.clientName}</span>
        <span className="contract-chip">{contract.engagementTypeName}</span>
        <span className="contract-chip">{contract.workModeName}</span>
        {timelineLabel && <span className="contract-chip accent">{timelineLabel}</span>}
        {contract.constraints.slice(0, expanded ? undefined : 2).map((item) => (
          <span key={item.code} className="contract-chip constraint">
            {item.name}
          </span>
        ))}
        {!expanded && contract.constraints.length > 2 && (
          <span className="contract-chip muted">+{contract.constraints.length - 2} more</span>
        )}
      </div>

      <div className="contract-brief-skills">
        <div className="contract-skill-row">
          <span className="contract-skill-label">Must-have</span>
          <div className="contract-skill-chips">
            {mustHave.map((skill) => (
              <span key={skill.name} className="contract-skill must">
                {skill.name}
              </span>
            ))}
          </div>
        </div>
        {expanded && niceToHave.length > 0 && (
          <div className="contract-skill-row">
            <span className="contract-skill-label">Nice-to-have</span>
            <div className="contract-skill-chips">
              {niceToHave.map((skill) => (
                <span key={skill.name} className="contract-skill nice">
                  {skill.name}
                </span>
              ))}
            </div>
          </div>
        )}
      </div>

      {expanded && (
        <div className="contract-brief-details">
          <div>
            <h3>Scope</h3>
            <p>{contract.scopeSummary}</p>
          </div>
          <div>
            <h3>Deliverables</h3>
            <ul>
              {contract.deliverables.map((item) => (
                <li key={item.id}>
                  <strong>{item.title}</strong>
                  {item.detail ? ` — ${item.detail}` : ''}
                </li>
              ))}
            </ul>
          </div>
          <div>
            <h3>Milestones</h3>
            <ul>
              {contract.milestones.map((item) => (
                <li key={item.id}>
                  <strong>{item.name}</strong>
                  {item.targetDate ? ` · ${formatDate(item.targetDate)}` : ''}
                  {item.description ? ` — ${item.description}` : ''}
                </li>
              ))}
            </ul>
          </div>
          <div>
            <h3>Constraints</h3>
            <ul>
              {contract.constraints.map((item) => (
                <li key={item.code}>{item.name}</li>
              ))}
            </ul>
          </div>
        </div>
      )}
    </section>
  )
}
