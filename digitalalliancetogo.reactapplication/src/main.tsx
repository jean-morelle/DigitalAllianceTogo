import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ThemeProvider } from 'next-themes';
import { Toaster } from '@/components/ui/sonner';
import { TooltipProvider } from '@/components/ui/tooltip';
import { ApiError } from '@/lib/api';
import { AuthProvider } from '@/lib/auth';
import './index.css';
import App from './App.tsx';

const queryClient = new QueryClient({
    defaultOptions: {
        queries: {
            staleTime: 30_000,
            // Inutile de réessayer une erreur métier (droits, introuvable, validation)
            retry: (tentatives, erreur) => !(erreur instanceof ApiError && erreur.status < 500) && tentatives < 2,
        },
    },
});

createRoot(document.getElementById('root')!).render(
    <StrictMode>
        <ThemeProvider attribute="class" defaultTheme="system" enableSystem>
            <QueryClientProvider client={queryClient}>
                <AuthProvider>
                    <TooltipProvider>
                        <App />
                        <Toaster richColors position="top-right" />
                    </TooltipProvider>
                </AuthProvider>
            </QueryClientProvider>
        </ThemeProvider>
    </StrictMode>,
);
