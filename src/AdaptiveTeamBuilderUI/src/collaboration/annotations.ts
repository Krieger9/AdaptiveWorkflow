import type { SignalsDisplayMode } from './types'

/** Screen-level cues the agent can trust without reverse-engineering the DOM. */
export const SELECT_CONTRACT_SCREEN_ANNOTATIONS: Record<string, string> = {
  purpose:
    'Shortlist a delivery engagement by comparing commercial signals, then select one to staff.',
  preferenceAxes: 'signalsDisplay, detailLevel',
  signalsDisplayOptions: 'values=numeric metrics; graph=relative bars across visible contracts',
  detailLevelOptions: 'summary=card signals only; extended=capacity/scope/skills after expand',
  adaptationGoal:
    'Infer whether this user prefers graph vs values and summary vs extended detail, then adapt the UI.',
}

export function contractCardAnnotations(input: {
  expanded: boolean
  signalsDisplay: SignalsDisplayMode | string
}): Record<string, string> {
  return {
    role: 'candidate-engagement',
    purpose:
      'Compare commercial signals; expand for staffing fit; select to proceed to team building.',
    detailLevel: input.expanded ? 'extended' : 'summary',
    signalsDisplay: input.signalsDisplay,
    decisionStage: 'portfolio-shortlist',
  }
}
