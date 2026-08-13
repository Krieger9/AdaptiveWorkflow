import type { CollaborationTendencyBundle } from './types'

export const APP_DOMAIN_DESCRIPTION =
  'We are an IT contracting company staffing delivery engagements from a portfolio of contracts.'

export const APP_DEFAULT_TENDENCY_PROSE =
  'On Select Contract, start with numeric signal values and summary cards. ' +
  'Expand a card for extended staffing/scope detail before selecting. ' +
  'No preferred commercial signal or graph-vs-values preference yet.'

export function createAppTendencyBundle(
  override?: Partial<CollaborationTendencyBundle> | null,
): CollaborationTendencyBundle {
  return {
    appDefaults: APP_DEFAULT_TENDENCY_PROSE,
    userOverride: null,
    updatedAt: null,
    source: 'app',
    recentTurnDigests: [],
    ...override,
  }
}
