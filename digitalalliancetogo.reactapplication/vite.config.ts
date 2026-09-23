import path from 'node:path';
import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [plugin(), tailwindcss()],
    resolve: {
        alias: {
            '@': path.resolve(__dirname, './src'),
        },
    },
    server: {
        port: 57547,
        // En développement, /api est relayé vers l'API ASP.NET Core (pas de CORS à configurer)
        proxy: {
            '/api': {
                target: process.env.API_URL ?? 'http://localhost:5104',
                changeOrigin: true,
            },
        },
    },
})
