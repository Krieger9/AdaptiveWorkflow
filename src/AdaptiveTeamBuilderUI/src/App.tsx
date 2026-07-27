import { useEffect, useState, type FormEvent } from 'react'
import { useIsAuthenticated, useMsal } from '@azure/msal-react'
import {
  InteractionRequiredAuthError,
  type AccountInfo,
} from '@azure/msal-browser'
import {
  ensureSession,
  greetingName,
  setAccessTokenProvider,
  updateMyProfile,
  verifyAccessToken,
  type User,
} from './api/client'
import { AuthCallback } from './auth/AuthCallback'
import { HomePage } from './components/HomePage'
import {
  AUTH_CALLBACK_PATH,
  AUTH_REDIRECT_URI,
  SPA_ORIGIN,
  apiScopes,
  entraConfigured,
  loginRequest,
} from './auth/msalConfig'
import './App.css'

type View = 'home' | 'edit'

function App() {
  const { instance, accounts } = useMsal()
  const isAuthenticated = useIsAuthenticated()
  const account = accounts[0] as AccountInfo | undefined

  const [view, setView] = useState<View>('home')
  const [homeKey, setHomeKey] = useState(0)
  const [user, setUser] = useState<User | null>(null)
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [loadingSession, setLoadingSession] = useState(false)
  const [path, setPath] = useState(window.location.pathname)

  useEffect(() => {
    const onNav = () => setPath(window.location.pathname)
    window.addEventListener('popstate', onNav)
    return () => window.removeEventListener('popstate', onNav)
  }, [])

  useEffect(() => {
    if (!isAuthenticated || !account) {
      setAccessTokenProvider(null)
      setUser(null)
      return
    }

    setAccessTokenProvider(async () => {
      try {
        const result = await instance.acquireTokenSilent({
          account,
          scopes: apiScopes,
        })
        return result.accessToken
      } catch (err) {
        if (err instanceof InteractionRequiredAuthError) {
          await instance.acquireTokenRedirect({
            account,
            scopes: apiScopes,
            redirectUri: AUTH_REDIRECT_URI,
          })
          throw new Error('Redirecting to acquire access token…')
        }
        throw err
      }
    })

    setLoadingSession(true)
    setError(null)

    ;(async () => {
      try {
        // Backend verifies JWT (issuer, audience, lifetime, signing key, scope).
        await verifyAccessToken()
        const sessionUser = await ensureSession()
        setUser(sessionUser)
        setView('home')
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to establish session')
        setUser(null)
      } finally {
        setLoadingSession(false)
      }
    })()
  }, [account, instance, isAuthenticated])

  async function handleSignIn() {
    setError(null)
    setBusy(true)
    try {
      // Redirect OAuth against Entra; access token is for AdaptiveTeamBuilderService.
      await instance.loginRedirect(loginRequest)
    } catch (err) {
      setBusy(false)
      setError(err instanceof Error ? err.message : 'Sign-in failed')
    }
  }

  async function handleSignOut() {
    setError(null)
    setBusy(true)
    try {
      setAccessTokenProvider(null)
      setUser(null)
      await instance.logoutRedirect({
        account,
        postLogoutRedirectUri: SPA_ORIGIN,
      })
    } catch (err) {
      setBusy(false)
      setError(err instanceof Error ? err.message : 'Sign-out failed')
    }
  }

  function openEdit() {
    if (!user) {
      return
    }
    setFirstName(user.firstName ?? '')
    setLastName(user.lastName ?? '')
    setDisplayName(user.displayName ?? '')
    setError(null)
    setView('edit')
  }

  async function handleSaveProfile(event: FormEvent) {
    event.preventDefault()
    if (!user) {
      return
    }
    setError(null)
    setBusy(true)
    try {
      const updated = await updateMyProfile({
        firstName: firstName.trim() || null,
        lastName: lastName.trim() || null,
        displayName: displayName.trim() || null,
      })
      setUser(updated)
      setView('home')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Save failed')
    } finally {
      setBusy(false)
    }
  }

  if (path.startsWith(AUTH_CALLBACK_PATH)) {
    return <AuthCallback />
  }

  if (!entraConfigured) {
    return (
      <main className="app">
        <section className="panel">
          <h1>Entra ID setup required</h1>
          <p className="muted">
            Copy <code>.env.example</code> to <code>.env.local</code> and set SPA
            registration values. API <code>AzureAd</code> must match the service app.
          </p>
        </section>
      </main>
    )
  }

  if (!isAuthenticated) {
    return (
      <main className="app">
        {error && <p className="error">{error}</p>}
        <section className="panel">
          <h1>Sign in</h1>
          <p className="muted">
            Sign in with Microsoft. The SPA requests an access token for{' '}
            <strong>AdaptiveTeamBuilderService</strong>; the API verifies that token
            before creating your session.
          </p>
          <button type="button" disabled={busy} onClick={handleSignIn}>
            {busy ? 'Redirecting…' : 'Sign in with Microsoft'}
          </button>
        </section>
      </main>
    )
  }

  if (loadingSession || !user) {
    return (
      <main className="app">
        {error && <p className="error">{error}</p>}
        <p className="muted">Verifying token with API…</p>
      </main>
    )
  }

  return (
    <div className="shell">
      <header className="topbar">
        <div className="brand">
          Adaptive Team Builder
          <span className="brand-user">Hello {greetingName(user)}</span>
        </div>
        <nav className="menu">
          <button
            type="button"
            className="linkish"
            onClick={() => {
              setError(null)
              setView('home')
              setHomeKey((value) => value + 1)
            }}
          >
            Home
          </button>
          <button type="button" className="linkish" onClick={openEdit}>
            My account
          </button>
          <button type="button" className="linkish" disabled={busy} onClick={handleSignOut}>
            Sign out
          </button>
        </nav>
      </header>

      <main className={view === 'home' ? 'app wide' : 'app'}>
        {error && <p className="error">{error}</p>}

        {view === 'home' && user && (
          <HomePage key={homeKey} userId={user.id} onError={setError} />
        )}

        {view === 'edit' && (
          <section className="panel">
            <h1>My account</h1>
            <p className="muted">Update first name, last name, and display name.</p>
            <form onSubmit={handleSaveProfile} className="form">
              <label>
                First name
                <input
                  value={firstName}
                  onChange={(e) => setFirstName(e.target.value)}
                  maxLength={100}
                />
              </label>
              <label>
                Last name
                <input
                  value={lastName}
                  onChange={(e) => setLastName(e.target.value)}
                  maxLength={100}
                />
              </label>
              <label>
                Display name
                <input
                  value={displayName}
                  onChange={(e) => setDisplayName(e.target.value)}
                  maxLength={200}
                />
              </label>
              <div className="actions">
                <button type="button" className="secondary" onClick={() => setView('home')}>
                  Cancel
                </button>
                <button type="submit" disabled={busy}>
                  {busy ? 'Saving…' : 'Save'}
                </button>
              </div>
            </form>
          </section>
        )}
      </main>
    </div>
  )
}

export default App
