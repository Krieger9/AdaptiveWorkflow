# Adaptive Team Builder

POC for agentic observation of user activity and adaptive interactive UI defaults.

## Auth model

- **SPA** (`AdaptiveTeamBuilder`) signs the user in with Entra through an MSAL v5 redirect bridge served at `/auth/callback`.
- SPA requests an **access token for the API** (`AdaptiveTeamBuilderService` scope `access_as_user`) — the backend is the resource/audience.
- **API** verifies JWT: issuer, audience, lifetime, signing keys, and required scope.
- On success, `POST /api/users/me/session` upserts `Users` by Entra `oid`.
- **SQL** uses LocalDB Windows integrated auth (`Trusted_Connection`), not the user token.

## Configured app IDs

| App | Client ID |
| --- | --- |
| SPA `AdaptiveTeamBuilder` | `21e97cb5-f8a6-4c88-8982-920c5624b715` |
| API `AdaptiveTeamBuilderService` | `7c2c18c8-2e26-412b-89a8-8055382a59d0` |
| Tenant | `a450efa3-cb51-43a1-b51e-44ff766fc1ac` |

SPA redirect URI: `http://localhost:5173/auth/callback`
API audience / scope: `api://7c2c18c8-2e26-412b-89a8-8055382a59d0/access_as_user`

Values live in:
- `src/AdaptiveTeamBuilderSvc/appsettings.Development.json` (`AzureAd`)
- `src/AdaptiveTeamBuilderUI/.env.local` (gitignored; see `.env.example`)

## Remaining Entra portal steps (required)

Do these once in Azure Portal → Microsoft Entra ID → App registrations:

### On **AdaptiveTeamBuilderService** (API)
1. **Expose an API** → set Application ID URI to `api://7c2c18c8-2e26-412b-89a8-8055382a59d0` (if not already).
2. Add scope **`access_as_user`** (Admins and users).
3. **Manifest / Authentication**: treat as a web API (no SPA redirect needed on this app).
4. Optional: **Expose an API → Authorized client applications** → add SPA client id `21e97cb5-f8a6-4c88-8982-920c5624b715` with `access_as_user` (pre-authorizes consent).

### On **AdaptiveTeamBuilder** (SPA)
1. **Authentication** → platform **Single-page application** → redirect URI exactly:
   `http://localhost:5173/auth/callback`
2. **API permissions** → Add permission → **My APIs** → `AdaptiveTeamBuilderService` → delegated `access_as_user`.
3. Click **Grant admin consent** for the tenant (if you can).

Without the exposed scope + SPA API permission, login may succeed in Entra but the API will reject the token (403 / invalid audience / missing scope).

Vite rewrites `/auth/callback` internally to the dedicated `redirect.html` MSAL
v5 bridge page while leaving the registered browser URL unchanged. This bridge
is used by redirect, popup, and silent iframe flows. In a hosted environment,
configure the same internal rewrite, serve the bridge and its bundled assets
from the same origin as the SPA, do not apply a
`Cross-Origin-Opener-Policy` header to it, and return `Cache-Control: no-store`.

## Schema deployment (DACPAC)

Schema is normalized for contractor profiles:

- `PositionTypes`, `ExperienceLevels`, `RoleSpecialties`, `Skills` (lookups)
- `EmployeeProfiles` (FKs to lookups)
- `EmployeeProfileSkills` (profile ↔ skill)

## Build & run (from repo root)

```powershell
# Build only
.\build-backend.ps1
.\build-frontend.ps1
.\build.ps1                    # both

# One-shell local dev (API background + UI foreground)
.\dev.ps1
.\dev.ps1 -Build               # build backend first, then run
.\dev.ps1 -Build -PublishDb    # also publish LocalDB schema

# Schema only
.\database\publish-local.ps1
```

`.\dev.ps1` starts the API on `http://localhost:5106`, waits for health, then runs Vite on `http://localhost:5173`. Ctrl+C stops both.

- UI: http://localhost:5173  
- Health: http://localhost:5106/health  
- Verified token claims: `GET /api/auth/me` (Bearer)  
- Session upsert: `POST /api/users/me/session` (Bearer)

## Collaboration run diagnostics

Every collaboration profile-update run writes two correlated artifacts:

- `src/AdaptiveTeamBuilderSvc/data/runs/{runId}.json` is the authoritative run record. It includes queue wait/coalescing, phase timings, profile versions and diff, every physical model attempt, token/cache/reasoning usage, validation errors, provider metadata, and failure details.
- `src/AdaptiveTeamBuilderSvc/logs/collaboration/*.md` contains the readable prompt/response transcript. Profile-updater filenames and headers include the same `runId`.

The agent call is currently non-streaming, so attempt timing is full wall time; time-to-first-token is not available. Failed processing after the model call is still written as an error run before the background service reports the exception.
