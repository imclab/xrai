import { defineConfig } from 'vite';
import compression from 'vite-plugin-compression';

export default defineConfig({
  server: {
    port: 3000,
    headers: {
      'Cross-Origin-Embedder-Policy': 'credentialless',
      'Cross-Origin-Opener-Policy': 'same-origin'
    }
  },
  optimizeDeps: {
    exclude: ['@needle-tools/engine']
  },
  plugins: [
    compression({
      algorithm: 'gzip',
      ext: '.gz'
    })
  ]
});