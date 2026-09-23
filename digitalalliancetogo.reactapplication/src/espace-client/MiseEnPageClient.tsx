import { Link, NavLink, Outlet } from 'react-router';
import { LogOut, ShoppingCart, User } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
    DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { useAuth } from '@/lib/auth';
import { Roles } from '@/lib/roles';
import { cn } from '@/lib/utils';
import { usePanier } from './panier';

/** Mise en page publique / client : boutique, panier, compte. Pensée d'abord pour le téléphone. */
export function MiseEnPageClient() {
    const { session, deconnecter } = useAuth();
    const { data: panier } = usePanier();
    const estClient = !!session?.roles.includes(Roles.Client);
    const estPersonnel = !!session && !estClient;

    const lien = ({ isActive }: { isActive: boolean }) =>
        cn('text-sm font-medium transition-colors', isActive ? 'text-foreground' : 'text-muted-foreground hover:text-foreground');

    return (
        <div className="bg-muted/30 flex min-h-svh flex-col">
            <header className="bg-background sticky top-0 z-10 border-b">
                <div className="mx-auto flex h-14 max-w-6xl items-center gap-4 px-4">
                    <Link to="/boutique" className="flex items-center gap-2">
                        <div className="bg-primary text-primary-foreground flex size-8 items-center justify-center rounded-md text-sm font-bold">TI</div>
                        <span className="hidden font-semibold sm:inline">Togo Informatique</span>
                    </Link>
                    <nav className="flex items-center gap-4">
                        <NavLink to="/boutique" end className={lien}>Boutique</NavLink>
                        {estClient && <NavLink to="/compte" className={lien}>Mon compte</NavLink>}
                        {estPersonnel && <Link to="/" className="text-muted-foreground hover:text-foreground text-sm">Back-office</Link>}
                    </nav>
                    <div className="flex-1" />

                    {estClient && (
                        <Button asChild variant="ghost" size="icon" className="relative" aria-label="Panier">
                            <Link to="/boutique/panier">
                                <ShoppingCart />
                                {!!panier?.nombreArticles && (
                                    <span className="bg-primary text-primary-foreground absolute -top-1 -right-1 flex min-w-5 items-center justify-center rounded-full px-1 text-xs">
                                        {panier.nombreArticles}
                                    </span>
                                )}
                            </Link>
                        </Button>
                    )}

                    {session ? (
                        <DropdownMenu>
                            <DropdownMenuTrigger asChild>
                                <Button variant="ghost" size="icon" aria-label="Mon compte"><User /></Button>
                            </DropdownMenuTrigger>
                            <DropdownMenuContent align="end" className="w-56">
                                <DropdownMenuLabel className="font-normal">
                                    <div className="text-sm font-medium">{session.prenom} {session.nom}</div>
                                    <div className="text-muted-foreground text-xs">{session.email}</div>
                                </DropdownMenuLabel>
                                <DropdownMenuSeparator />
                                {estClient && <DropdownMenuItem asChild><Link to="/compte">Mes commandes</Link></DropdownMenuItem>}
                                {estClient && <DropdownMenuItem asChild><Link to="/compte/adresses">Mes adresses</Link></DropdownMenuItem>}
                                <DropdownMenuItem onClick={deconnecter}><LogOut /> Se déconnecter</DropdownMenuItem>
                            </DropdownMenuContent>
                        </DropdownMenu>
                    ) : (
                        <div className="flex gap-2">
                            <Button asChild variant="ghost" size="sm"><Link to="/connexion">Connexion</Link></Button>
                            <Button asChild size="sm"><Link to="/inscription">Créer un compte</Link></Button>
                        </div>
                    )}
                </div>
            </header>

            <main className="mx-auto w-full max-w-6xl flex-1 p-4 md:p-6">
                <Outlet />
            </main>

            <footer className="text-muted-foreground border-t py-6 text-center text-xs">
                Togo Informatique · Lomé · Paiement Mobile Money (T-Money, Flooz) · Livraison à domicile
            </footer>
        </div>
    );
}
