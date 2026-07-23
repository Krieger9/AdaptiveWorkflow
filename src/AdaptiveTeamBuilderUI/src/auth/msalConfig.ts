import {
  type Configuration,
  type RedirectRequest,
  LogLevel,
  PublicClientApplication,
} from '@azure/msal-browser'

const clientId = import.meta.env.VITE_AZURE_CLIENT_ID as string | undefined
const tenantId = import.meta.env.VITE_AZURE_TENANT_ID as string | undefined
const apiScope = import.meta.env.VITE_AZURE_API_SCOPE as string | undefined

export const AUTH_CALLBACK_PATH = '/auth/callback'

/** Must match the SPA redirect URI registered in Entra exactly. */
export const SPA_ORIGIN = 'http://localhost:5173'
export const AUTH_REDIRECT_URI = `${SPA_ORIGIN}${AUTH_CALLBACK_PATH}`

export const entraConfigured =
  Boolean(clientId) &&
  Boolean(tenantId) &&
  Boolean(apiScope) &&
  !clientId!.startsWith('YOUR_') &&
  !tenantId!.startsWith('YOUR_') &&
  !apiScope!.startsWith('YOUR_')

export const msalConfig: Configuration = {
  auth: {
    clientId: entraConfigured ? clientId! : '00000000-0000-0000-0000-000000000000',
    authority: `https://login.microsoftonline.com/${entraConfigured ? tenantId! : 'common'}`,
    redirectUri: AUTH_REDIRECT_URI,
    postLogoutRedirectUri: SPA_ORIGIN,
  },
  cache: {
    cacheLocation: 'localStorage',
  },
  system: {
    loggerOptions: {
      logLevel: LogLevel.Warning,
    },
  },
}

/** Access tokens are requested for the backend API (resource), not the SPA client. */
export const apiScopes = entraConfigured ? [apiScope!] : []

export const loginRequest: RedirectRequest = {
  scopes: apiScopes,
  redirectUri: AUTH_REDIRECT_URI,
}

export const msalInstance = new PublicClientApplication(msalConfig)
