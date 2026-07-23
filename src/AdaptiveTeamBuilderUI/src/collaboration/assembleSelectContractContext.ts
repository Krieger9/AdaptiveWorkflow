import type { ContractDetail, ContractListItem } from '../api/client'
import { APP_DOMAIN_DESCRIPTION, createAppTendencyBundle } from './appDefaults'
import {
  SELECT_CONTRACT_SCREEN_ID,
  type CollaborationAdviseRequest,
  type CollaborationControlSnapshot,
  type CollaborationInteractionEvent,
  type CollaborationTendencyBundle,
} from './types'

function money(value: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    maximumFractionDigits: 0,
  }).format(value)
}

function datasetSummary(item: ContractListItem, index: number): string {
  return (
    `${index + 1}. ${item.code} — ${item.title} (${item.clientName}); ` +
    `value ${money(item.estimatedContractValue)}, profit ${money(item.estimatedProfit)}, ` +
    `win ${item.winProbabilityPercent}%, delivery risk ${item.deliveryRiskName}`
  )
}

function toControlSnapshot(
  item: ContractListItem,
  expandedId: string | null,
  detailsById: Record<string, ContractDetail>,
): CollaborationControlSnapshot {
  const detail = detailsById[item.id]
  const data: Record<string, string> = {
    code: item.code,
    clientName: item.clientName,
    title: item.title,
    outcomeSummary: item.outcomeSummary,
    estimatedContractValue: String(item.estimatedContractValue),
    estimatedProfit: String(item.estimatedProfit),
    estimatedMarginPercent: String(item.estimatedMarginPercent),
    winProbabilityPercent: String(item.winProbabilityPercent),
    deliveryRisk: item.deliveryRiskName,
    strategicValue: item.strategicValueName,
    engagementType: item.engagementTypeName,
    workMode: item.workModeName,
    teamCount: String(item.teamCount),
  }

  let detailData: Record<string, string> | null = null
  if (detail) {
    detailData = {
      staffingFte: String(detail.staffingFte),
      scopeSummary: detail.scopeSummary,
      mustHaveSkills: detail.skills
        .filter((s) => s.priority === 'MustHave')
        .map((s) => s.name)
        .join(', '),
      constraints: detail.constraints.map((c) => c.name).join(', '),
    }
    if (detail.specialistStaffingNeeded) {
      detailData.specialistStaffingNeeded = detail.specialistStaffingNeeded
    }
    if (detail.durationWeeks != null) {
      detailData.durationWeeks = String(detail.durationWeeks)
    }
  }

  return {
    controlId: item.id,
    controlType: 'contract-card',
    label: `${item.code} ${item.title}`,
    expanded: expandedId === item.id,
    data,
    detailData,
  }
}

export function assembleSelectContractContext(input: {
  contracts: ContractListItem[]
  expandedId: string | null
  detailsById: Record<string, ContractDetail>
  events: CollaborationInteractionEvent[]
  tendencies?: CollaborationTendencyBundle | null
}): CollaborationAdviseRequest {
  const controls = input.contracts.map((item) =>
    toControlSnapshot(item, input.expandedId, input.detailsById),
  )

  return {
    app: {
      domainDescription: APP_DOMAIN_DESCRIPTION,
      contractCount: input.contracts.length,
      datasetSummaries: input.contracts.map(datasetSummary),
    },
    screen: {
      screenId: SELECT_CONTRACT_SCREEN_ID,
      title: 'Select a contract',
      availableActions: [
        'expand',
        'collapse',
        'select',
        'navigate-to-contract',
        'signal-focus',
      ],
    },
    controls,
    events: input.events,
    tendencies: input.tendencies ?? createAppTendencyBundle(),
  }
}
