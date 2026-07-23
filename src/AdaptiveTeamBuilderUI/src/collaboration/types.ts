export type TendencySource = 'app' | 'stub' | 'llm'

export type CollaborationAppContext = {
  domainDescription: string
  contractCount: number
  datasetSummaries: string[]
}

export type CollaborationScreenContext = {
  screenId: string
  title: string
  availableActions: string[]
}

export type CollaborationControlSnapshot = {
  controlId: string
  controlType: string
  label: string
  expanded: boolean
  data: Record<string, string>
  detailData?: Record<string, string> | null
}

export type InteractionEventType =
  | 'screen.enter'
  | 'screen.leave'
  | 'control.expand'
  | 'control.collapse'
  | 'control.select'
  | 'signal.focus'

export type CollaborationInteractionEvent = {
  at: string
  screenId: string
  type: InteractionEventType | string
  controlId?: string | null
  label?: string | null
  meta?: Record<string, string> | null
}

export type CollaborationTendencyBundle = {
  appDefaults: string
  userOverride: string | null
  updatedAt: string | null
  source: TendencySource | string
}

export type CollaborationAdviseRequest = {
  app: CollaborationAppContext
  screen: CollaborationScreenContext
  controls: CollaborationControlSnapshot[]
  events: CollaborationInteractionEvent[]
  tendencies: CollaborationTendencyBundle
}

export type CollaborationSuggestion = {
  id: string
  kind: string
  label: string
  targetControlId: string | null
}

export type CollaborationAdviseResponse = {
  promptPreview: string
  updatedTendencies: CollaborationTendencyBundle
  suggestions: CollaborationSuggestion[]
}

export type CollaborationTendenciesResponse = {
  tendencies: CollaborationTendencyBundle
}

export const SELECT_CONTRACT_SCREEN_ID = 'select-contract'
