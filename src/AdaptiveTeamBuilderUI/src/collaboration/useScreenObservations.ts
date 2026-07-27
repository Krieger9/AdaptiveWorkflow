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
    recordObservation({
      screenId,
      type: 'screen.enter',
      meta: { meaning: 'entered-select-contract' },
    })
    return () => {
      recordObservation({
        screenId,
        type: 'screen.leave',
        meta: { meaning: 'left-select-contract' },
      })
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
    (
      controlId: string,
      signalId: string,
      signalLabel: string,
      options?: { controlLabel?: string; signalsDisplay?: string },
    ) => {
      const key = `${controlId}:${signalId}`
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
        label: signalLabel,
        meta: {
          signalId,
          signalLabel,
          meaning: 'inspected-commercial-signal',
          ...(options?.signalsDisplay
            ? { signalsDisplay: options.signalsDisplay }
            : {}),
          ...(options?.controlLabel ? { controlLabel: options.controlLabel } : {}),
        },
      })
    },
    [emit],
  )

  const emitSignalActivate = useCallback(
    (
      controlId: string,
      signalId: string,
      signalLabel: string,
      options?: { controlLabel?: string; signalsDisplay?: string },
    ) => {
      return emit('signal.activate', {
        controlId,
        label: signalLabel,
        meta: {
          signalId,
          signalLabel,
          activation: 'dblclick',
          meaning: 'activated-commercial-signal',
          ...(options?.signalsDisplay
            ? { signalsDisplay: options.signalsDisplay }
            : {}),
          ...(options?.controlLabel ? { controlLabel: options.controlLabel } : {}),
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

  return { emit, emitSignalFocus, emitSignalActivate, peek, drain }
}
