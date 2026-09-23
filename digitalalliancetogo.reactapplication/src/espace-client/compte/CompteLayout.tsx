import { NavLink, Outlet } from 'react-router';
import { useAuth } from '@/lib/auth';
import { cn } from '@/lib/utils';

const ONGLETS: [string, string][] = [
    ['/compte', 'Mes commandes'],
    ['/compte/devis', 'Mes devis'],
    ['/compte/sav', 'Service après-vente'],
    ['/compte/avoirs', 'Mes avoirs'],
    ['/compte/adresses', 'Mes adresses'],
];

export function CompteLayout() {
    const { session } = useAuth();
    return (
        <>
            <h1 className="text-2xl font-semibold">Bonjour {session?.prenom || session?.nom}</h1>
            <nav className="my-4 flex gap-1 overflow-x-auto border-b">
                {ONGLETS.map(([chemin, libelle]) => (
                    <NavLink key={chemin} to={chemin} end={chemin === '/compte'}
                        className={({ isActive }) => cn('border-b-2 px-3 py-2 text-sm font-medium whitespace-nowrap',
                            isActive ? 'border-primary text-foreground' : 'text-muted-foreground hover:text-foreground border-transparent')}>
                        {libelle}
                    </NavLink>
                ))}
            </nav>
            <Outlet />
        </>
    );
}
