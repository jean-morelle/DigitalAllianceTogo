import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router';
import { useAuth } from '@/lib/auth';
import { Roles, type Role } from '@/lib/roles';
import { NAVIGATION } from './navigation';

/** Back-office : exige une session du personnel ; un client est renvoyé vers la boutique. */
export function RequireAuth({ children }: { children: ReactNode }) {
    const { session } = useAuth();
    const location = useLocation();

    // Visiteur sur la racine du site : la vitrine publique ; ailleurs : connexion puis retour
    if (!session) return location.pathname === '/'
        ? <Navigate to="/boutique" replace />
        : <Navigate to="/connexion" replace state={{ depuis: location.pathname }} />;

    // Un client n'a rien à faire au back-office : direction son espace
    if (!session.roles.some(r => r !== Roles.Client)) return <Navigate to="/boutique" replace />;
    return <>{children}</>;
}

/** Espace client : exige un compte client connecté. */
export function RequireClient({ children }: { children: ReactNode }) {
    const { session } = useAuth();
    const location = useLocation();
    if (!session) return <Navigate to="/connexion" replace state={{ depuis: location.pathname }} />;
    if (!session.roles.includes(Roles.Client)) return <Navigate to="/" replace />;
    return <>{children}</>;
}

/** Page réservée à certains rôles ; sinon, retour à la première page autorisée. */
export function RequireRole({ roles, children }: { roles: Role[]; children: ReactNode }) {
    const { aRole } = useAuth();
    return aRole(...roles) ? <>{children}</> : <RedirectionAccueil />;
}

/** Première entrée du menu autorisée (ex : le Livreur arrive sur ses livraisons). */
export function RedirectionAccueil() {
    const { aRole } = useAuth();
    const premiere = NAVIGATION.find(e => aRole(...e.roles));
    return premiere && premiere.chemin !== '/'
        ? <Navigate to={premiere.chemin} replace />
        : <p className="text-muted-foreground">Aucun écran n'est disponible pour votre rôle.</p>;
}
