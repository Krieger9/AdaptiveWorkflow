import type { CollaborationInteractionEvent, InteractionEventType } from './types'

const buffer: CollaborationInteractionEvent[] = []

/** Last change-action timestamp per screen (excludes signal.focus / screen.enter/leave). */
const lastChangeAtByScreen = new Map<string, number>()

const CHANGE_ACTION_TYPES = new Set([
  'control.expand',
  'control.collapse',
  'control.select',
  'view.change',
  'signal.activate',
])

export type RecordObservationInput = {
  screenId: string
  type: InteractionEventType | string
  controlId?: string | null
  label?: string | null
  meta?: Record<string, string> | null
  at?: string
}

export function isChangeActionType(type: string): boolean {
  return CHANGE_ACTION_TYPES.has(type)
}

export function recordObservation(input: RecordObservationInput): CollaborationInteractionEvent {
  const at = input.at ?? new Date().toISOString()
  const atMs = Date.parse(at)
  const meta: Record<string, string> = { ...(input.meta ?? {}) }

  if (isChangeActionType(input.type) && !Number.isNaN(atMs)) {
    const previous = lastChangeAtByScreen.get(input.screenId)
    if (previous != null) {
      meta.sincePreviousMs = String(Math.max(0, atMs - previous))
    }
    lastChangeAtByScreen.set(input.screenId, atMs)
  }

  const event: CollaborationInteractionEvent = {
    at,
    screenId: input.screenId,
    type: input.type,
    controlId: input.controlId ?? null,
    label: input.label ?? null,
    meta: Object.keys(meta).length > 0 ? meta : null,
  }
  buffer.push(event)
  return event
}

export function peekObservations(screenId?: string): CollaborationInteractionEvent[] {
  if (!screenId) {
    return [...buffer]
  }
  return buffer.filter((event) => event.screenId === screenId)
}

/** Removes and returns matching events (all events if screenId omitted). */
export function drainObservations(screenId?: string): CollaborationInteractionEvent[] {
  if (!screenId) {
    const all = [...buffer]
    buffer.length = 0
    lastChangeAtByScreen.clear()
    return all
  }

  const kept: CollaborationInteractionEvent[] = []
  const drained: CollaborationInteractionEvent[] = []
  for (const event of buffer) {
    if (event.screenId === screenId) {
      drained.push(event)
    } else {
      kept.push(event)
    }
  }
  buffer.length = 0
  buffer.push(...kept)
  lastChangeAtByScreen.delete(screenId)
  return drained
}

export function clearObservations(screenId?: string): void {
  if (!screenId) {
    buffer.length = 0
    lastChangeAtByScreen.clear()
    return
  }
  const kept = buffer.filter((event) => event.screenId !== screenId)
  buffer.length = 0
  buffer.push(...kept)
  lastChangeAtByScreen.delete(screenId)
}
