import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { api, CLE_SESSION } from '@/lib/api';
import { Roles, type Role } from '@/lib/roles';

export interface Session {
    token: string;
    expiresAtUtc: string;
    utilisateurId: string;
    nom: string;
    prenom: string;
    email: string;
    roles: string[];
}

interface AuthContexte {
    session: Session | null;
    connecter: (email: string, motDePasse: string) => Promise<Session>;
    deconnecter: () => void;
    /** Vrai si l'utilisateur a l'un des rôles (l'Admin a tous les droits). */
    aRole: (...roles: Role[]) => boolean;
}

const Contexte = createContext<AuthContexte | null>(null);

function lireSession(): Session | null {
    try {
        const brut = localStorage.getItem(CLE_SESSION);
        if (!brut) return null;
        const session = JSON.parse(brut) as Session;
        return new Date(session.expiresAtUtc).getTime() > Date.now() ? session : null;
    } catch {
        return null;
    }
}

export function AuthProvider({ children }: { children: ReactNode }) {
    const [session, setSession] = useState<Session | null>(lireSession);

    const deconnecter = useCallback(() => {
        try {
            localStorage.removeItem(CLE_SESSION);
        } catch {
            /* stockage indisponible */
        }
        setSession(null);
    }, []);

    const connecter = useCallback(async (email: string, motDePasse: string) => {
        const resultat = await api.post<Session>('/auth/login', { email, motDePasse });
        try {
            localStorage.setItem(CLE_SESSION, JSON.stringify(resultat));
        } catch {
            /* stockage indisponible : session valable jusqu'au rechargement */
        }
        setSession(resultat);
        return resultat;
    }, []);

    // Jeton refusé par l'API (401) ou arrivé à expiration : retour à la connexion
    useEffect(() => {
        const surExpiration = () => deconnecter();
        window.addEventListener('dat:session-expiree', surExpiration);
        const delai = session ? new Date(session.expiresAtUtc).getTime() - Date.now() : 0;
        const minuteur = session && delai > 0 ? window.setTimeout(deconnecter, delai) : undefined;
        return () => {
            window.removeEventListener('dat:session-expiree', surExpiration);
            window.clearTimeout(minuteur);
        };
    }, [session, deconnecter]);

    const aRole = useCallback(
        (...roles: Role[]) => !!session && (session.roles.includes(Roles.Admin) || roles.some(r => session.roles.includes(r))),
        [session],
    );

    const valeur = useMemo(() => ({ session, connecter, deconnecter, aRole }), [session, connecter, deconnecter, aRole]);
    return <Contexte.Provider value={valeur}>{children}</Contexte.Provider>;
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth(): AuthContexte {
    const contexte = useContext(Contexte);
    if (!contexte) throw new Error('useAuth doit être utilisé dans AuthProvider');
    return contexte;
}
