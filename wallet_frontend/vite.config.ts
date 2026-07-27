import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig(async ({ mode }) => {
  const plugins = [react(), tailwindcss()]

  try {
    // @ts-ignore
    const m = await import('./.vite-source-tags.js')
    plugins.push(m.sourceTags())
  } catch {}

  const env = loadEnv(mode, process.cwd(), ['VITE_', 'NEXT_PUBLIC_'])
  const backend = env.VITE_API_BASE_URL || process.env.BACKEND || 'https://campus-pay-na3y.onrender.com'

  return {
    plugins,
    envPrefix: ['VITE_', 'NEXT_PUBLIC_'],
    server: {
      proxy: {
        '/api': {
          target: backend,
          changeOrigin: true,
          secure: false,
        },
      },
    },
  }
})
