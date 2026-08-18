export type BeliefSource = 'app' | 'stub' | 'llm'

export type SignalsDisplayMode = 'values' | 'graph'

/** Why a state change happened. Only `user` interactions are evidence of preference. */
export type Causation = 'user' | 'system-default' | 'restored' | 'agent-applied'

export type CollaborationAppContext = {
  domainDescription: string
  itemCount: number
  datasetSummaries: string[]
}

export type CollaborationViewState = {
  signalsDisplay: SignalsDisplayMode | string
  expandedControlIds: string[]
}

/**
 * Context of the surface an interaction batch happened on, assembled from the
 * surface registry by depth-first traversal.
 */
export type SurfaceContext = {
  /** Ordered surface ids from root to leaf, e.g. ['page:contracts', 'section:contracts.list']. */
  surfacePath: string[]
  title: string
  availableActions: string[]
  viewState: CollaborationViewState
  /** Deterministic prose generated from the surface tree (purpose + inherited domain). */
  assembledContext?: string | null
  /** Hash of assembledContext so the harness can tell context changes from prompt changes. */
  contextHash?: string | null
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

/** Attribute snapshot of the entity an interaction targeted. */
export type EntityRef = {
  type: string
  id: string
  attrs: Record<string, string>
}

/** One alternative that was visible and available at the moment of an interaction. */
export type ChoiceSetItem = {
  id: string
  attrs: Record<string, string>
}

/**
 * Semantic interaction actions for the collaboration agent.
 * Prefer meaning-bearing actions + meta over raw click descriptions.
 */
export type InteractionAction =
  | 'surface.enter'
  | 'surface.leave'
  | 'view.change'
  | 'control.expand'
  | 'control.collapse'
  | 'control.select'
  | 'signal.focus'
  | 'signal.activate'

/** One logged user or system act against a surface. The atomic unit of evidence. */
export type Interaction = {
  id: string
  at: string
  sessionId: string
  /** Monotonic sequence within the session. */
  seq: number
  /** Ordered surface ids from root to leaf. */
  surfacePath: string[]
  action: InteractionAction | string
  controlId?: string | null
  label?: string | null
  valueBefore?: string | null
  valueAfter?: string | null
  causation: Causation | string
  /** Set server-side when a user act undoes a recent agent-applied state. */
  reversal?: boolean | null
  entity?: EntityRef | null
  /** Alternatives visible at the moment of the interaction. Enables negatives. */
  choiceSet?: ChoiceSetItem[] | null
  meta?: Record<string, string> | null
}

/**
 * The markdown belief document for one tier plus recent decision-turn digests.
 */
export type BeliefProfile = {
  /** control | application | universal */
  tier: string
  /** The full markdown belief document. */
  document: string
  source: BeliefSource | string
  version: number
  updatedAt: string | null
  /** Newest-last compact digests of recent decision turns (max ~5). */
  recentTurnDigests?: string[] | null
}

export type CollaborationAdviseRequest = {
  app: CollaborationAppContext
  surface: SurfaceContext
  controls: CollaborationControlSnapshot[]
  interactions: Interaction[]
}

export type SuggestionKind = 'expand' | 'collapse' | 'select' | 'set-view' | string

export type CollaborationSuggestion = {
  id: string
  kind: SuggestionKind
  label: string
  targetControlId: string | null
  payload?: Record<string, string> | null
  /** Preference dimension the suggestion draws on, when known. */
  dimension?: string | null
  /** True when issued to resolve agent uncertainty rather than high conviction. */
  isProbe?: boolean
  rationale?: string | null
}

export type CollaborationPreferredLayout = {
  expandAll: boolean
  signalsDisplay?: string | null
  rationale?: string | null
  /** When expandAll is false, expand this many cards by signal or concrete expand suggestions. */
  expandTopCount?: number | null
  /** Margin | Profit | Value | Win prob.; may be null while ranking signals remain correlated. */
  expandBySignal?: string | null
}

export type CollaborationAdviseResponse = {
  promptPreview: string
  suggestions: CollaborationSuggestion[]
  preferredLayout?: CollaborationPreferredLayout | null
}

export type CollaborationProfileResponse = {
  profile: BeliefProfile
}

export type CollaborationObservationsRequest = {
  userId: string
  sessionId: string
  app: CollaborationAppContext
  surface: SurfaceContext
  controls: CollaborationControlSnapshot[]
  interactions: Interaction[]
}

export type CollaborationObservationsResponse = {
  userId: string
  acceptedInteractionCount: number
  status: string
  promptPreview: string
  suggestions: CollaborationSuggestion[]
  preferredLayout?: CollaborationPreferredLayout | null
}

/** Surface ids for the contracts page (kind:dotted.name). */
export const CONTRACTS_PAGE_SURFACE_ID = 'page:contracts'
export const CONTRACTS_LIST_SURFACE_ID = 'section:contracts.list'
