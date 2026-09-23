import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router';
import { ShieldAlert } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/lib/auth';
import { Roles, type Role } from '@/lib/roles';
import { NAVIGATION } from './navigation';

/** Exige une session ; un Client (sans rôle interne) n'a pas accès au back-office. */
export function RequireAuth({ children }: { children: ReactNode }) {
    const { session, deconnecter } = useAuth();
    const location = useLocation();

    if (!session) return <Navigate to="/connexion" replace state={{ depuis: location.pathname }} />;

    const estPersonnel = session.roles.some(r => r !== Roles.Client);
    if (!estPersonnel) {
        return (
            <div className="flex min-h-svh flex-col items-center justify-center gap-4 p-6 text-center">
                <ShieldAlert className="text-muted-foreground size-10" />
                <p className="max-w-sm">Cet espace est réservé au personnel de Togo Informatique. L'espace client arrive bientôt.</p>
                <Button variant="outline" onClick={deconnecter}>Se déconnecter</Button>
            </div>
        );
    }
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
