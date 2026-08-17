import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { MsalProvider } from '@azure/msal-react'
import { msalInstance } from './auth/msalConfig'
import './index.css'
import App from './App.tsx'

async function start() {
  await msalInstance.initialize()
  // The MSAL v5 redirect bridge returns redirect flows to the page where they
  // started. Complete the cached authorization response before rendering.
  await msalInstance.handleRedirectPromise()

  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <MsalProvider instance={msalInstance}>
        <App />
      </MsalProvider>
    </StrictMode>,
  )
}

void start()
