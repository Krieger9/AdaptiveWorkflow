import type { CollaborationTendencyBundle } from './types'

export const APP_DOMAIN_DESCRIPTION =
  'We are an IT contracting company staffing delivery engagements from a portfolio of contracts.'

export const APP_DEFAULT_TENDENCY_PROSE =
  'On Select Contract, examine cards left-to-right in grid order. Expand details before choosing. No preferred commercial signal yet.'

export function createAppTendencyBundle(
  override?: Partial<CollaborationTendencyBundle> | null,
): CollaborationTendencyBundle {
  return {
    appDefaults: APP_DEFAULT_TENDENCY_PROSE,
    userOverride: null,
    updatedAt: null,
    source: 'app',
    ...override,
  }
}
