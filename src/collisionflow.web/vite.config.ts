import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],

  // The SPA builds straight into the API's wwwroot, so the whole application
  // ships as one artifact to one App Service. One origin also means no CORS
  // configuration to get wrong.
  build: {
    outDir: '../CollisionFlow.Api/wwwroot',
    emptyOutDir: true,
    sourcemap: true,
  },

  server: {
    port: 5173,
    // In development the two run separately; this proxy keeps the browser
    // talking to a single origin so the code is identical in both modes.
    proxy: {
      '/api': {
        target: 'http://localhost:5210',
        changeOrigin: true,
      },
    },
  },
})
