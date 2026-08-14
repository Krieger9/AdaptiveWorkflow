import { getSurfaceChildren, getSurfaceNode } from './surface'

/**
 * Deterministic prose assembled from the surface registry: depth-first traversal,
 * inherited domain printed once at the shallowest declaring surface. The output is
 * stable for a stable registry, so its hash separates context changes from prompt
 * changes in run records.
 */
export function assembleSurfaceContext(rootId: string): string {
  const lines: string[] = []
  visit(rootId, 0, false)
  return lines.join('\n')

  function visit(id: string, depth: number, domainPrinted: boolean) {
    const node = getSurfaceNode(id)
    if (!node) {
      return
    }

    const indent = '  '.repeat(depth)
    lines.push(`${indent}[${node.id}]${node.title ? ` ${node.title}` : ''}`)
    lines.push(`${indent}  purpose: ${node.purpose}`)

    let printed = domainPrinted
    if (node.domain && !printed) {
      lines.push(`${indent}  domain: ${node.domain}`)
      printed = true
    }

    if (node.annotations) {
      for (const key of Object.keys(node.annotations).sort()) {
        lines.push(`${indent}  ${key}: ${node.annotations[key]}`)
      }
    }

    for (const child of getSurfaceChildren(id)) {
      visit(child.id, depth + 1, printed)
    }
  }
}

/** FNV-1a 32-bit hash, hex encoded — enough to tell "same context" from "changed". */
export function hashContext(text: string): string {
  let hash = 0x811c9dc5
  for (let i = 0; i < text.length; i++) {
    hash ^= text.charCodeAt(i)
    hash = Math.imul(hash, 0x01000193)
  }
  return (hash >>> 0).toString(16).padStart(8, '0')
}
