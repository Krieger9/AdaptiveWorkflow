export const apiBaseUrl =
  import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5106'

export type User = {
  id: string
  azureAdObjectId: string
  userName: string
  firstName: string | null
  lastName: string | null
  displayName: string | null
  createdDate: string
  modifiedDate: string
  lastLoggedInDate: string | null
}

export type AuthMe = {
  authenticated: boolean
  objectId: string | null
  name: string | null
  userName: string | null
  audience: string | null
  issuer: string | null
  scopes: string | null
}

type TokenProvider = () => Promise<string>

let tokenProvider: TokenProvider | null = null

export function setAccessTokenProvider(provider: TokenProvider | null) {
  tokenProvider = provider
}

async function authHeaders(): Promise<HeadersInit> {
  if (!tokenProvider) {
    throw new Error('Not signed in.')
  }
  const token = await tokenProvider()
  return {
    Authorization: `Bearer ${token}`,
    'Content-Type': 'application/json',
  }
}

async function parseJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.text()
    throw new Error(body || `Request failed: ${response.status}`)
  }
  return response.json() as Promise<T>
}

/** Calls the API so the backend verifies the access token (signature, audience, scope). */
export async function verifyAccessToken(): Promise<AuthMe> {
  const response = await fetch(`${apiBaseUrl}/api/auth/me`, {
    headers: await authHeaders(),
  })
  return parseJson<AuthMe>(response)
}

export async function ensureSession(): Promise<User> {
  const response = await fetch(`${apiBaseUrl}/api/users/me/session`, {
    method: 'POST',
    headers: await authHeaders(),
  })
  return parseJson<User>(response)
}

export async function getMe(): Promise<User> {
  const response = await fetch(`${apiBaseUrl}/api/users/me`, {
    headers: await authHeaders(),
  })
  return parseJson<User>(response)
}

export async function updateMyProfile(profile: {
  firstName: string | null
  lastName: string | null
  displayName: string | null
}): Promise<User> {
  const response = await fetch(`${apiBaseUrl}/api/users/me/profile`, {
    method: 'PUT',
    headers: await authHeaders(),
    body: JSON.stringify(profile),
  })
  return parseJson<User>(response)
}

export type PositionType =
  | 'Developer'
  | 'UxDesigner'
  | 'Product'
  | 'QualityAssurance'

export type EmployeeProfileListItem = {
  id: string
  displayName: string
  positionType: PositionType
  roleSpecialty: string | null
  level: string | null
  title: string
  location: string | null
  availability: string | null
}

export type EmployeeProfile = {
  id: string
  firstName: string
  lastName: string
  displayName: string
  positionType: PositionType
  roleSpecialty: string | null
  level: string | null
  title: string
  summary: string | null
  skills: string[]
  yearsExperience: number | null
  location: string | null
  availability: string | null
  createdDate: string
  modifiedDate: string
}

export async function searchEmployeeProfiles(options: {
  q?: string
  positionTypes?: PositionType[]
  teamId?: string | null
}): Promise<EmployeeProfileListItem[]> {
  const params = new URLSearchParams()
  if (options.q?.trim()) {
    params.set('q', options.q.trim())
  }
  if (options.positionTypes && options.positionTypes.length > 0) {
    params.set('positionTypes', options.positionTypes.join(','))
  }
  if (options.teamId) {
    params.set('teamId', options.teamId)
  }

  const query = params.toString()
  const response = await fetch(
    `${apiBaseUrl}/api/profiles${query ? `?${query}` : ''}`,
    { headers: await authHeaders() },
  )
  return parseJson<EmployeeProfileListItem[]>(response)
}

export async function getEmployeeProfile(id: string): Promise<EmployeeProfile> {
  const response = await fetch(`${apiBaseUrl}/api/profiles/${id}`, {
    headers: await authHeaders(),
  })
  return parseJson<EmployeeProfile>(response)
}

export type TeamListItem = {
  id: string
  name: string
  contractId: string
}

export type TeamRequirement = {
  positionType: PositionType
  positionTypeName: string
  requiredCount: number
  selectedCount: number
}

export type TeamMember = {
  employeeProfileId: string
  firstName: string
  lastName: string
  displayName: string
  positionType: PositionType
  roleSpecialty: string | null
  level: string | null
  title: string
}

export type TeamDetail = {
  id: string
  name: string
  contractId: string
  createdDate: string
  modifiedDate: string
  requirements: TeamRequirement[]
  members: TeamMember[]
  hiddenEmployeeProfileIds: string[]
}

export type ContractListItem = {
  id: string
  code: string
  title: string
  clientName: string
  outcomeSummary: string
  engagementType: string
  engagementTypeName: string
  workMode: string
  workModeName: string
  durationWeeks: number | null
  targetDeliveryDate: string | null
  estimatedContractValue: number
  estimatedProfit: number
  estimatedMarginPercent: number
  winProbabilityPercent: number
  deliveryRisk: string
  deliveryRiskName: string
  strategicValue: string
  strategicValueName: string
  staffingFte: number
  specialistStaffingNeeded: string | null
  expectedProfit: number
  riskAdjustedProfit: number
  profitPerMonth: number | null
  profitPerFte: number
  teamCount: number
}

export type ContractSkill = {
  name: string
  priority: string
  priorityName: string
}

export type ContractConstraint = {
  code: string
  name: string
}

export type ContractDeliverable = {
  id: string
  sortOrder: number
  title: string
  detail: string | null
}

export type ContractMilestone = {
  id: string
  sortOrder: number
  name: string
  targetDate: string | null
  description: string | null
}

export type ContractDetail = {
  id: string
  code: string
  title: string
  clientName: string
  outcomeSummary: string
  scopeSummary: string
  engagementType: string
  engagementTypeName: string
  workMode: string
  workModeName: string
  durationWeeks: number | null
  startDate: string | null
  targetDeliveryDate: string | null
  estimatedContractValue: number
  estimatedProfit: number
  estimatedMarginPercent: number
  winProbabilityPercent: number
  deliveryRisk: string
  deliveryRiskName: string
  strategicValue: string
  strategicValueName: string
  staffingFte: number
  specialistStaffingNeeded: string | null
  expectedProfit: number
  riskAdjustedProfit: number
  profitPerMonth: number | null
  profitPerFte: number
  isDefault: boolean
  skills: ContractSkill[]
  constraints: ContractConstraint[]
  deliverables: ContractDeliverable[]
  milestones: ContractMilestone[]
}

export async function listContracts(): Promise<ContractListItem[]> {
  const response = await fetch(`${apiBaseUrl}/api/contracts`, {
    headers: await authHeaders(),
  })
  return parseJson<ContractListItem[]>(response)
}

export async function getDefaultContract(): Promise<ContractDetail> {
  const response = await fetch(`${apiBaseUrl}/api/contracts/default`, {
    headers: await authHeaders(),
  })
  return parseJson<ContractDetail>(response)
}

export async function getContract(id: string): Promise<ContractDetail> {
  const response = await fetch(`${apiBaseUrl}/api/contracts/${id}`, {
    headers: await authHeaders(),
  })
  return parseJson<ContractDetail>(response)
}

export async function listTeams(contractId: string): Promise<TeamListItem[]> {
  const params = new URLSearchParams({ contractId })
  const response = await fetch(`${apiBaseUrl}/api/teams?${params}`, {
    headers: await authHeaders(),
  })
  return parseJson<TeamListItem[]>(response)
}

export async function createTeam(name: string, contractId: string): Promise<TeamDetail> {
  const response = await fetch(`${apiBaseUrl}/api/teams`, {
    method: 'POST',
    headers: await authHeaders(),
    body: JSON.stringify({ name, contractId }),
  })
  return parseJson<TeamDetail>(response)
}

export async function getTeam(id: string): Promise<TeamDetail> {
  const response = await fetch(`${apiBaseUrl}/api/teams/${id}`, {
    headers: await authHeaders(),
  })
  return parseJson<TeamDetail>(response)
}

export async function renameTeam(id: string, name: string): Promise<TeamDetail> {
  const response = await fetch(`${apiBaseUrl}/api/teams/${id}`, {
    method: 'PUT',
    headers: await authHeaders(),
    body: JSON.stringify({ name }),
  })
  return parseJson<TeamDetail>(response)
}

export async function upsertTeamRequirements(
  teamId: string,
  requirements: { positionType: PositionType; requiredCount: number }[],
): Promise<TeamDetail> {
  const response = await fetch(`${apiBaseUrl}/api/teams/${teamId}/requirements`, {
    method: 'PUT',
    headers: await authHeaders(),
    body: JSON.stringify({ requirements }),
  })
  return parseJson<TeamDetail>(response)
}

export async function addTeamMember(
  teamId: string,
  employeeProfileId: string,
): Promise<TeamDetail> {
  const response = await fetch(`${apiBaseUrl}/api/teams/${teamId}/members`, {
    method: 'POST',
    headers: await authHeaders(),
    body: JSON.stringify({ employeeProfileId }),
  })
  return parseJson<TeamDetail>(response)
}

export async function removeTeamMember(
  teamId: string,
  employeeProfileId: string,
): Promise<TeamDetail> {
  const response = await fetch(
    `${apiBaseUrl}/api/teams/${teamId}/members/${employeeProfileId}`,
    {
      method: 'DELETE',
      headers: await authHeaders(),
    },
  )
  return parseJson<TeamDetail>(response)
}

export async function hideTeamProfile(
  teamId: string,
  employeeProfileId: string,
): Promise<TeamDetail> {
  const response = await fetch(`${apiBaseUrl}/api/teams/${teamId}/hidden`, {
    method: 'POST',
    headers: await authHeaders(),
    body: JSON.stringify({ employeeProfileId }),
  })
  return parseJson<TeamDetail>(response)
}

export function greetingName(user: User): string {
  const display = user.displayName?.trim()
  if (display) {
    return display
  }
  return user.userName
}

export type CollaborationAdviseRequest = {
  app: {
    domainDescription: string
    contractCount: number
    datasetSummaries: string[]
  }
  screen: {
    screenId: string
    title: string
    availableActions: string[]
    viewState: {
      signalsDisplay: string
      expandedControlIds: string[]
    }
    annotations?: Record<string, string> | null
  }
  controls: {
    controlId: string
    controlType: string
    label: string
    expanded: boolean
    data: Record<string, string>
    detailData?: Record<string, string> | null
    annotations?: Record<string, string> | null
  }[]
  events: {
    at: string
    screenId: string
    type: string
    controlId?: string | null
    label?: string | null
    meta?: Record<string, string> | null
  }[]
}

export type CollaborationAdviseResponse = {
  promptPreview: string
  suggestions: {
    id: string
    kind: string
    label: string
    targetControlId: string | null
    payload?: Record<string, string> | null
  }[]
  preferredLayout?: {
    expandAll: boolean
    signalsDisplay?: string | null
    rationale?: string | null
  } | null
}

export type CollaborationProfileResponse = {
  tendencies: {
    appDefaults: string
    userOverride: string | null
    updatedAt: string | null
    source: string
  }
}

export type CollaborationObservationsRequest = {
  userId: string
  app: CollaborationAdviseRequest['app']
  screen: CollaborationAdviseRequest['screen']
  controls: CollaborationAdviseRequest['controls']
  events: CollaborationAdviseRequest['events']
}

export type CollaborationObservationsResponse = {
  userId: string
  acceptedEventCount: number
  status: string
  promptPreview: string
  suggestions: CollaborationAdviseResponse['suggestions']
  preferredLayout?: CollaborationAdviseResponse['preferredLayout']
}

export async function getCollaborationProfile(): Promise<CollaborationProfileResponse> {
  const response = await fetch(`${apiBaseUrl}/api/collaboration/profile`, {
    headers: await authHeaders(),
  })
  return parseJson<CollaborationProfileResponse>(response)
}

export async function adviseCollaboration(
  request: CollaborationAdviseRequest,
): Promise<CollaborationAdviseResponse> {
  const response = await fetch(`${apiBaseUrl}/api/collaboration/advise`, {
    method: 'POST',
    headers: await authHeaders(),
    body: JSON.stringify(request),
  })
  return parseJson<CollaborationAdviseResponse>(response)
}

export async function submitCollaborationObservations(
  request: CollaborationObservationsRequest,
): Promise<CollaborationObservationsResponse> {
  const response = await fetch(`${apiBaseUrl}/api/collaboration/observations`, {
    method: 'POST',
    headers: await authHeaders(),
    body: JSON.stringify(request),
  })
  return parseJson<CollaborationObservationsResponse>(response)
}

