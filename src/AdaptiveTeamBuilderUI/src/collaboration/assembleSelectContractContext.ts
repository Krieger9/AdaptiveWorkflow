import type { ContractDetail, ContractListItem } from '../api/client'
import {
  SELECT_CONTRACT_SCREEN_ANNOTATIONS,
  contractCardAnnotations,
} from './annotations'
import { APP_DOMAIN_DESCRIPTION } from './appDefaults'
import {
  SELECT_CONTRACT_SCREEN_ID,
  type CollaborationAdviseRequest,
  type CollaborationControlSnapshot,
  type CollaborationInteractionEvent,
  type CollaborationObservationsRequest,
  type SignalsDisplayMode,
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
  expandedIds: ReadonlySet<string>,
  detailsById: Record<string, ContractDetail>,
  signalsDisplay: SignalsDisplayMode | string,
): CollaborationControlSnapshot {
  const expanded = expandedIds.has(item.id)
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
    expanded,
    data,
    detailData,
    annotations: contractCardAnnotations({ expanded, signalsDisplay }),
  }
}

function assemblePageContext(input: {
  contracts: ContractListItem[]
  expandedIds: ReadonlySet<string>
  detailsById: Record<string, ContractDetail>
  events: CollaborationInteractionEvent[]
  signalsDisplay: SignalsDisplayMode | string
}): CollaborationAdviseRequest {
  const controls = input.contracts.map((item) =>
    toControlSnapshot(
      item,
      input.expandedIds,
      input.detailsById,
      input.signalsDisplay,
    ),
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
        'set-signals-display',
        'expand-detail',
        'collapse-detail',
        'select-contract',
        'inspect-signal',
      ],
      viewState: {
        signalsDisplay: input.signalsDisplay,
        expandedControlIds: [...input.expandedIds],
      },
      annotations: SELECT_CONTRACT_SCREEN_ANNOTATIONS,
    },
    controls,
    events: input.events,
  }
}

export function assembleSelectContractContext(input: {
  contracts: ContractListItem[]
  expandedIds: ReadonlySet<string>
  detailsById: Record<string, ContractDetail>
  events: CollaborationInteractionEvent[]
  signalsDisplay: SignalsDisplayMode | string
}): CollaborationAdviseRequest {
  return assemblePageContext(input)
}

export function assembleSelectContractObservations(input: {
  userId: string
  contracts: ContractListItem[]
  expandedIds: ReadonlySet<string>
  detailsById: Record<string, ContractDetail>
  events: CollaborationInteractionEvent[]
  signalsDisplay: SignalsDisplayMode | string
}): CollaborationObservationsRequest {
  const page = assemblePageContext(input)
  return {
    userId: input.userId,
    app: page.app,
    screen: page.screen,
    controls: page.controls,
    events: page.events,
  }
}
