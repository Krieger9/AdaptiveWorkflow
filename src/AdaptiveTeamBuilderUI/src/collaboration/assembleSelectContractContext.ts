import type { ContractDetail, ContractListItem } from '../api/client'
import {
  SELECT_CONTRACT_SCREEN_ANNOTATIONS,
  contractCardAnnotations,
} from './annotations'
import { APP_DOMAIN_DESCRIPTION } from './appDefaults'
import { assembleSurfaceContext, hashContext } from './assembleSurfaceContext'
import {
  CONTRACTS_LIST_SURFACE_ID,
  CONTRACTS_PAGE_SURFACE_ID,
  type ChoiceSetItem,
  type CollaborationAdviseRequest,
  type CollaborationControlSnapshot,
  type CollaborationObservationsRequest,
  type Interaction,
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
    `margin ${item.estimatedMarginPercent}%, win ${item.winProbabilityPercent}%, ` +
    `delivery risk ${item.deliveryRiskName}`
  )
}

/** Compact attribute snapshot used for interaction choice sets. */
export function contractChoiceSetItem(item: ContractListItem): ChoiceSetItem {
  return {
    id: item.id,
    attrs: {
      code: item.code,
      title: item.title,
      estimatedContractValue: String(item.estimatedContractValue),
      estimatedProfit: String(item.estimatedProfit),
      estimatedMarginPercent: String(item.estimatedMarginPercent),
      winProbabilityPercent: String(item.winProbabilityPercent),
      deliveryRisk: item.deliveryRiskName,
    },
  }
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

type AssembleInput = {
  contracts: ContractListItem[]
  expandedIds: ReadonlySet<string>
  detailsById: Record<string, ContractDetail>
  interactions: Interaction[]
  signalsDisplay: SignalsDisplayMode | string
}

function assemblePageContext(input: AssembleInput): CollaborationAdviseRequest {
  const controls = input.contracts.map((item) =>
    toControlSnapshot(
      item,
      input.expandedIds,
      input.detailsById,
      input.signalsDisplay,
    ),
  )

  // Registry-driven generic assembler; the surface tree is declared by <Surface> wrappers.
  const assembledContext = assembleSurfaceContext(CONTRACTS_PAGE_SURFACE_ID)

  return {
    app: {
      domainDescription: APP_DOMAIN_DESCRIPTION,
      itemCount: input.contracts.length,
      datasetSummaries: input.contracts.map(datasetSummary),
    },
    surface: {
      surfacePath: [CONTRACTS_PAGE_SURFACE_ID, CONTRACTS_LIST_SURFACE_ID],
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
      assembledContext: assembledContext || null,
      contextHash: assembledContext ? hashContext(assembledContext) : null,
      annotations: SELECT_CONTRACT_SCREEN_ANNOTATIONS,
    },
    controls,
    interactions: input.interactions,
  }
}

export function assembleSelectContractContext(
  input: AssembleInput,
): CollaborationAdviseRequest {
  return assemblePageContext(input)
}

export function assembleSelectContractObservations(
  input: AssembleInput & { userId: string; sessionId: string },
): CollaborationObservationsRequest {
  const page = assemblePageContext(input)
  return {
    userId: input.userId,
    sessionId: input.sessionId,
    app: page.app,
    surface: page.surface,
    controls: page.controls,
    interactions: page.interactions,
  }
}
