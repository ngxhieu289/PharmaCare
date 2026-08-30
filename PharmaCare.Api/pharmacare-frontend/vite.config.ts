import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const rootDirectory = path.dirname(fileURLToPath(import.meta.url))

export default defineConfig({
  plugins: [react()],
  resolve: { alias: { '@': path.resolve(rootDirectory, './src') } },
  server: {
    port: 3000,
    proxy: {
      '/api': {
        // Port 5000 is commonly occupied by macOS ControlCenter/AirPlay.
        // PharmaCare's launch profile uses 5080.
        target: process.env.VITE_BACKEND_URL ?? 'http://127.0.0.1:5080',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
