import { defineConfig, type Plugin } from 'vite'
import react from '@vitejs/plugin-react'
import { fileURLToPath } from 'node:url'

const AUTH_CALLBACK_PATH = '/auth/callback'
const REDIRECT_BRIDGE_PATH = '/redirect.html'

function authCallbackRewrite(): Plugin {
  const rewrite = (url: string | undefined) => {
    if (!url) {
      return url
    }

    const parsed = new URL(url, 'http://localhost')
    if (parsed.pathname !== AUTH_CALLBACK_PATH) {
      return url
    }

    return `${REDIRECT_BRIDGE_PATH}${parsed.search}`
  }

  return {
    name: 'auth-callback-redirect-bridge',
    configureServer(server) {
      server.middlewares.use((request, _response, next) => {
        request.url = rewrite(request.url)
        next()
      })
    },
    configurePreviewServer(server) {
      server.middlewares.use((request, _response, next) => {
        request.url = rewrite(request.url)
        next()
      })
    },
  }
}

export default defineConfig({
  plugins: [authCallbackRewrite(), react()],
  appType: 'spa',
  build: {
    rollupOptions: {
      input: {
        main: fileURLToPath(new URL('./index.html', import.meta.url)),
        redirect: fileURLToPath(new URL('./redirect.html', import.meta.url)),
      },
    },
  },
  server: {
    host: 'localhost',
    port: 5173,
    strictPort: true,
  },
})
