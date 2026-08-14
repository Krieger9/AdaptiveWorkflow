import { createContext, useContext, useEffect, useMemo, type ReactNode } from 'react'
import type { ChoiceSetItem, EntityRef } from './types'

/**
 * A surface is a region of UI that declares a purpose. Any new page gets
 * instrumentation by wrapping with <Surface>, not by hand-writing an assembler.
 */
export type SurfaceDescriptor = {
  /** `kind:dotted.name`, e.g. `page:contracts` or `section:contracts.list`. */
  id: string
  /** One or two sentences on what this surface is for. Feeds the assembled context. */
  purpose: string
  /** Domain prose, inherited by descendants; printed once at the shallowest declaring surface. */
  domain?: string
  title?: string
  /** Extra prose annotations the agent can trust without reverse-engineering the DOM. */
  annotations?: Record<string, string>
}

type RegistryNode = SurfaceDescriptor & {
  parentId: string | null
  /** Registration order for deterministic traversal. */
  order: number
}

// Module-level registry so the assembler can traverse outside the React tree.
const registry = new Map<string, RegistryNode>()
let registrationCounter = 0

export function getSurfaceNode(id: string): RegistryNode | undefined {
  return registry.get(id)
}

export function getSurfaceChildren(parentId: string | null): RegistryNode[] {
  return [...registry.values()]
    .filter((node) => node.parentId === parentId)
    .sort((a, b) => a.order - b.order)
}

type SurfaceContextValue = {
  /** Ordered surface ids from root to this surface. */
  surfacePath: string[]
  /** Entity the nearest surface represents (e.g. one contract card). */
  entity: EntityRef | null
  /** Alternatives visible on the nearest enclosing choice surface. */
  choiceSet: ChoiceSetItem[] | null
}

const SurfaceReactContext = createContext<SurfaceContextValue>({
  surfacePath: [],
  entity: null,
  choiceSet: null,
})

export type SurfaceProps = SurfaceDescriptor & {
  /** Entity this surface represents, e.g. a single contract card. */
  entity?: EntityRef | null
  /** The visible alternatives when this surface presents a choice (sibling entities). */
  choiceSet?: ChoiceSetItem[] | null
  children: ReactNode
}

/**
 * Declares a surface. Nesting builds the surface path via React context, and the
 * module registry powers the generic context assembler.
 */
export function Surface({
  id,
  purpose,
  domain,
  title,
  annotations,
  entity,
  choiceSet,
  children,
}: SurfaceProps) {
  const parent = useContext(SurfaceReactContext)
  const parentId = parent.surfacePath.length > 0
    ? parent.surfacePath[parent.surfacePath.length - 1]
    : null

  // Register synchronously so assembly works on first render; refresh on prop change.
  if (!registry.has(id)) {
    registry.set(id, { id, purpose, domain, title, annotations, parentId, order: registrationCounter++ })
  } else {
    const existing = registry.get(id)!
    registry.set(id, { ...existing, purpose, domain, title, annotations, parentId })
  }

  useEffect(() => {
    return () => {
      registry.delete(id)
    }
  }, [id])

  const value = useMemo<SurfaceContextValue>(
    () => ({
      surfacePath: [...parent.surfacePath, id],
      entity: entity ?? parent.entity,
      choiceSet: choiceSet ?? parent.choiceSet,
    }),
    [parent, id, entity, choiceSet],
  )

  return (
    <SurfaceReactContext.Provider value={value}>{children}</SurfaceReactContext.Provider>
  )
}

/** Reads the enclosing surface path plus entity/choice-set context. */
export function useSurface(): SurfaceContextValue {
  return useContext(SurfaceReactContext)
}
