import type { CollaborationInteractionEvent, InteractionEventType } from './types'

const buffer: CollaborationInteractionEvent[] = []

export type RecordObservationInput = {
  screenId: string
  type: InteractionEventType | string
  controlId?: string | null
  label?: string | null
  meta?: Record<string, string> | null
  at?: string
}

export function recordObservation(input: RecordObservationInput): CollaborationInteractionEvent {
  const event: CollaborationInteractionEvent = {
    at: input.at ?? new Date().toISOString(),
    screenId: input.screenId,
    type: input.type,
    controlId: input.controlId ?? null,
    label: input.label ?? null,
    meta: input.meta ?? null,
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
  return drained
}

export function clearObservations(screenId?: string): void {
  if (!screenId) {
    buffer.length = 0
    return
  }
  const kept = buffer.filter((event) => event.screenId !== screenId)
  buffer.length = 0
  buffer.push(...kept)
}
