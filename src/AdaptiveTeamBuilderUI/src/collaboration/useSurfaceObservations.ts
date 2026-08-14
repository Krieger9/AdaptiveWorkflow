import { useCallback, useEffect, useRef } from 'react'
import {
  drainInteractions,
  peekInteractions,
  recordInteraction,
  type RecordInteractionInput,
} from './observationBuffer'
import { useSurface } from './surface'
import type { Interaction, InteractionAction } from './types'

const SIGNAL_FOCUS_DEBOUNCE_MS = 500

type EmitOptions = Omit<RecordInteractionInput, 'surfacePath' | 'action'>

/**
 * Observation hook bound to the enclosing <Surface>. Emitted interactions
 * automatically carry the full surface path and the visible choice set.
 */
export function useSurfaceObservations() {
  const { surfacePath, entity, choiceSet } = useSurface()
  const surfaceKey = surfacePath.join(' / ')
  const lastSignalKeyRef = useRef<string | null>(null)
  const lastSignalAtRef = useRef(0)
  const contextRef = useRef({ surfacePath, entity, choiceSet })
  contextRef.current = { surfacePath, entity, choiceSet }

  useEffect(() => {
    recordInteraction({
      surfacePath: contextRef.current.surfacePath,
      action: 'surface.enter',
      meta: { meaning: 'entered-surface' },
    })
    return () => {
      recordInteraction({
        surfacePath: contextRef.current.surfacePath,
        action: 'surface.leave',
        meta: { meaning: 'left-surface' },
      })
    }
    // Only re-run when the surface identity itself changes.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [surfaceKey])

  const emit = useCallback(
    (action: InteractionAction | string, options?: EmitOptions) => {
      const context = contextRef.current
      return recordInteraction({
        surfacePath: context.surfacePath,
        action,
        entity: options?.entity ?? context.entity,
        choiceSet: options?.choiceSet ?? context.choiceSet,
        ...options,
      })
    },
    [],
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
    (): Interaction[] => peekInteractions(contextRef.current.surfacePath),
    [],
  )

  const drain = useCallback(
    (): Interaction[] => drainInteractions(contextRef.current.surfacePath),
    [],
  )

  return { surfacePath, emit, emitSignalFocus, emitSignalActivate, peek, drain }
}
