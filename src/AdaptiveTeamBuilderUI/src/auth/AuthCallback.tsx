import { useEffect } from 'react'
import { useMsal } from '@azure/msal-react'

/** Completes the Entra redirect and returns the SPA to the app root. */
export function AuthCallback() {
  const { instance } = useMsal()

  useEffect(() => {
    let active = true

    async function finish() {
      try {
        await instance.handleRedirectPromise()
      } finally {
        if (active && window.location.pathname !== '/') {
          window.history.replaceState({}, document.title, '/')
          window.dispatchEvent(new PopStateEvent('popstate'))
        }
      }
    }

    void finish()
    return () => {
      active = false
    }
  }, [instance])

  return (
    <main className="app">
      <p className="muted">Completing sign-in…</p>
    </main>
  )
}
