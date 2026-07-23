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
}): Promise<EmployeeProfileListItem[]> {
  const params = new URLSearchParams()
  if (options.q?.trim()) {
    params.set('q', options.q.trim())
  }
  if (options.positionTypes && options.positionTypes.length > 0) {
    params.set('positionTypes', options.positionTypes.join(','))
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

export function greetingName(user: User): string {
  const display = user.displayName?.trim()
  if (display) {
    return display
  }
  return user.userName
}
