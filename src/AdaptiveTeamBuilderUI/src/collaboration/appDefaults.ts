import type { BeliefProfile } from './types'

export const APP_DOMAIN_DESCRIPTION =
  'We are an IT contracting company staffing delivery engagements from a portfolio of contracts.'

/** Placeholder profile shown before the server responds (server seeds the real default). */
export function createDefaultBeliefProfile(
  override?: Partial<BeliefProfile> | null,
): BeliefProfile {
  return {
    tier: 'control',
    document:
      '# Control-Tier Profile\n\n(No beliefs yet — the app defaults to numeric signal values ' +
      'and collapsed summary cards. Expand a card for extended staffing/scope detail before selecting.)',
    source: 'app',
    version: 0,
    updatedAt: null,
    recentTurnDigests: [],
    ...override,
  }
}
