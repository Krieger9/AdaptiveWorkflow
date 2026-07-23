import { useCallback, useEffect, useRef } from 'react'
import {
  drainObservations,
  peekObservations,
  recordObservation,
  type RecordObservationInput,
} from './observationBuffer'
import type { CollaborationInteractionEvent, InteractionEventType } from './types'

const SIGNAL_FOCUS_DEBOUNCE_MS = 500

export function useScreenObservations(screenId: string) {
  const lastSignalKeyRef = useRef<string | null>(null)
  const lastSignalAtRef = useRef(0)

  useEffect(() => {
    recordObservation({ screenId, type: 'screen.enter' })
    return () => {
      recordObservation({ screenId, type: 'screen.leave' })
    }
  }, [screenId])

  const emit = useCallback(
    (
      type: InteractionEventType | string,
      options?: Omit<RecordObservationInput, 'screenId' | 'type'>,
    ) => {
      return recordObservation({
        screenId,
        type,
        ...options,
      })
    },
    [screenId],
  )

  const emitSignalFocus = useCallback(
    (controlId: string, signal: string, controlLabel?: string) => {
      const key = `${controlId}:${signal}`
      const now = Date.now()
      if (
        lastSignalKeyRef.current === key &&
        now - lastSignalAtRef.current < SIGNAL_FOCUS_DEBOUNCE_MS
      ) {
        return null
      }
      lastSignalKeyRef.current = key
      lastSignalAtRef.current = now
      return emit('signal.focus', {
        controlId,
        label: signal,
        meta: {
          signal,
          ...(controlLabel ? { controlLabel } : {}),
        },
      })
    },
    [emit],
  )

  const peek = useCallback(
    (): CollaborationInteractionEvent[] => peekObservations(screenId),
    [screenId],
  )

  const drain = useCallback(
    (): CollaborationInteractionEvent[] => drainObservations(screenId),
    [screenId],
  )

  return { emit, emitSignalFocus, peek, drain }
}
