import type {
  Causation,
  ChoiceSetItem,
  EntityRef,
  Interaction,
  InteractionAction,
} from './types'

/** One session per app load; interactions carry it for replay grouping. */
export const sessionId = createSessionId()

let sequence = 0

const buffer: Interaction[] = []

/** Last change-action timestamp per surface path (excludes signal.focus / surface.enter/leave). */
const lastChangeAtBySurface = new Map<string, number>()

const CHANGE_ACTIONS = new Set([
  'control.expand',
  'control.collapse',
  'control.select',
  'view.change',
  'signal.activate',
])

function randomSuffix(length: number): string {
  const bytes = crypto.getRandomValues(new Uint8Array(length))
  return Array.from(bytes, (b) => (b % 36).toString(36)).join('')
}

function createSessionId(): string {
  const now = new Date()
  const stamp =
    `${now.getUTCFullYear()}${String(now.getUTCMonth() + 1).padStart(2, '0')}` +
    `${String(now.getUTCDate()).padStart(2, '0')}t` +
    `${String(now.getUTCHours()).padStart(2, '0')}${String(now.getUTCMinutes()).padStart(2, '0')}`
  return `s_${stamp}_${randomSuffix(4)}`
}

export type RecordInteractionInput = {
  surfacePath: string[]
  action: InteractionAction | string
  controlId?: string | null
  label?: string | null
  valueBefore?: string | null
  valueAfter?: string | null
  /** Defaults to 'user'; agent-applied / restored / system-default must be explicit. */
  causation?: Causation | string
  entity?: EntityRef | null
  choiceSet?: ChoiceSetItem[] | null
  meta?: Record<string, string> | null
  at?: string
}

export function isChangeAction(action: string): boolean {
  return CHANGE_ACTIONS.has(action)
}

export function recordInteraction(input: RecordInteractionInput): Interaction {
  const at = input.at ?? new Date().toISOString()
  const atMs = Date.parse(at)
  const surfaceKey = input.surfacePath.join(' / ')
  const meta: Record<string, string> = { ...(input.meta ?? {}) }
  const causation = input.causation ?? 'user'

  // Timing gaps only measure the user's own pace, not system-applied changes.
  if (causation === 'user' && isChangeAction(input.action) && !Number.isNaN(atMs)) {
    const previous = lastChangeAtBySurface.get(surfaceKey)
    if (previous != null) {
      meta.sincePreviousMs = String(Math.max(0, atMs - previous))
    }
    lastChangeAtBySurface.set(surfaceKey, atMs)
  }

  sequence += 1
  const interaction: Interaction = {
    id: `i_${sequence}_${randomSuffix(6)}`,
    at,
    sessionId,
    seq: sequence,
    surfacePath: [...input.surfacePath],
    action: input.action,
    controlId: input.controlId ?? null,
    label: input.label ?? null,
    valueBefore: input.valueBefore ?? null,
    valueAfter: input.valueAfter ?? null,
    causation,
    entity: input.entity ?? null,
    choiceSet: input.choiceSet ?? null,
    meta: Object.keys(meta).length > 0 ? meta : null,
  }
  buffer.push(interaction)
  return interaction
}

function matchesSurface(interaction: Interaction, surfacePathPrefix?: string[]): boolean {
  if (!surfacePathPrefix || surfacePathPrefix.length === 0) {
    return true
  }
  return surfacePathPrefix.every((id, index) => interaction.surfacePath[index] === id)
}

export function peekInteractions(surfacePathPrefix?: string[]): Interaction[] {
  return buffer.filter((interaction) => matchesSurface(interaction, surfacePathPrefix))
}

/** Removes and returns matching interactions (all if no prefix given). */
export function drainInteractions(surfacePathPrefix?: string[]): Interaction[] {
  if (!surfacePathPrefix || surfacePathPrefix.length === 0) {
    const all = [...buffer]
    buffer.length = 0
    lastChangeAtBySurface.clear()
    return all
  }

  const kept: Interaction[] = []
  const drained: Interaction[] = []
  for (const interaction of buffer) {
    if (matchesSurface(interaction, surfacePathPrefix)) {
      drained.push(interaction)
    } else {
      kept.push(interaction)
    }
  }
  buffer.length = 0
  buffer.push(...kept)
  return drained
}

export function clearInteractions(surfacePathPrefix?: string[]): void {
  if (!surfacePathPrefix || surfacePathPrefix.length === 0) {
    buffer.length = 0
    lastChangeAtBySurface.clear()
    return
  }
  const kept = buffer.filter((interaction) => !matchesSurface(interaction, surfacePathPrefix))
  buffer.length = 0
  buffer.push(...kept)
}
