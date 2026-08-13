export type TendencySource = 'app' | 'stub' | 'llm'

export type SignalsDisplayMode = 'values' | 'graph'

export type CollaborationAppContext = {
  domainDescription: string
  contractCount: number
  datasetSummaries: string[]
}

export type CollaborationViewState = {
  signalsDisplay: SignalsDisplayMode | string
  expandedControlIds: string[]
}

export type CollaborationScreenContext = {
  screenId: string
  title: string
  availableActions: string[]
  viewState: CollaborationViewState
  annotations?: Record<string, string> | null
}

export type CollaborationControlSnapshot = {
  controlId: string
  controlType: string
  label: string
  expanded: boolean
  data: Record<string, string>
  detailData?: Record<string, string> | null
  annotations?: Record<string, string> | null
}

/**
 * Semantic interaction types for the collaboration agent.
 * Prefer meaning-bearing types + meta over raw click descriptions.
 */
export type InteractionEventType =
  | 'screen.enter'
  | 'screen.leave'
  | 'view.change'
  | 'control.expand'
  | 'control.collapse'
  | 'control.select'
  | 'signal.focus'
  | 'signal.activate'

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
  /** Newest-last compact digests of recent decision turns (max ~5). */
  recentTurnDigests?: string[] | null
}

export type CollaborationAdviseRequest = {
  app: CollaborationAppContext
  screen: CollaborationScreenContext
  controls: CollaborationControlSnapshot[]
  events: CollaborationInteractionEvent[]
}

export type SuggestionKind = 'expand' | 'collapse' | 'select' | 'set-view' | string

export type CollaborationSuggestion = {
  id: string
  kind: SuggestionKind
  label: string
  targetControlId: string | null
  payload?: Record<string, string> | null
}

export type CollaborationPreferredLayout = {
  expandAll: boolean
  signalsDisplay?: string | null
  rationale?: string | null
  /** When expandAll is false, expand this many highest-ranked cards by expandBySignal. */
  expandTopCount?: number | null
  /** Margin | Profit | Value | Win prob. */
  expandBySignal?: string | null
}

export type CollaborationAdviseResponse = {
  promptPreview: string
  suggestions: CollaborationSuggestion[]
  preferredLayout?: CollaborationPreferredLayout | null
}

export type CollaborationProfileResponse = {
  tendencies: CollaborationTendencyBundle
}

export type CollaborationObservationsRequest = {
  userId: string
  app: CollaborationAppContext
  screen: CollaborationScreenContext
  controls: CollaborationControlSnapshot[]
  events: CollaborationInteractionEvent[]
}

export type CollaborationObservationsResponse = {
  userId: string
  acceptedEventCount: number
  status: string
  promptPreview: string
  suggestions: CollaborationSuggestion[]
  preferredLayout?: CollaborationPreferredLayout | null
}

export const SELECT_CONTRACT_SCREEN_ID = 'select-contract'
