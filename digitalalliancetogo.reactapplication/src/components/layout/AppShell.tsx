import { useState } from 'react';
import { NavLink, Outlet } from 'react-router';
import { LogOut, Menu, Moon, Sun } from 'lucide-react';
import { useTheme } from 'next-themes';
import { Button } from '@/components/ui/button';
import {
    DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Sheet, SheetContent, SheetTitle, SheetTrigger } from '@/components/ui/sheet';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { useAuth } from '@/lib/auth';
import { cn } from '@/lib/utils';
import { NAVIGATION } from './navigation';

function Menu_({ surClic }: { surClic?: () => void }) {
    const { aRole } = useAuth();
    return (
        <nav className="flex flex-col gap-1">
            {NAVIGATION.filter(e => aRole(...e.roles)).map(({ chemin, libelle, icone: Icone }) => (
                <NavLink
                    key={chemin}
                    to={chemin}
                    end={chemin === '/'}
                    onClick={surClic}
                    className={({ isActive }) => cn(
                        'flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors',
                        isActive ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-muted hover:text-foreground',
                    )}
                >
                    <Icone className="size-4" />
                    {libelle}
                </NavLink>
            ))}
        </nav>
    );
}

function Marque() {
    return (
        <div className="flex items-center gap-2 px-3 py-4">
            <div className="bg-primary text-primary-foreground flex size-8 items-center justify-center rounded-md text-sm font-bold">TI</div>
            <div className="leading-tight">
                <div className="text-sm font-semibold">Togo Informatique</div>
                <div className="text-muted-foreground text-xs">Back-office</div>
            </div>
        </div>
    );
}

/** Mise en page du back-office : menu latéral (tiroir sur mobile), en-tête utilisateur, contenu. */
export function AppShell() {
    const { session, deconnecter } = useAuth();
    const { resolvedTheme, setTheme } = useTheme();
    const [menuMobile, setMenuMobile] = useState(false);
    const initiales = `${session?.prenom?.[0] ?? ''}${session?.nom?.[0] ?? ''}`.toUpperCase();

    return (
        <div className="bg-muted/30 flex min-h-svh">
            <aside className="bg-background hidden w-64 shrink-0 border-r p-3 md:block">
                <Marque />
                <Menu_ />
            </aside>

            <div className="flex min-w-0 flex-1 flex-col">
                <header className="bg-background sticky top-0 z-10 flex h-14 items-center gap-2 border-b px-4">
                    <Sheet open={menuMobile} onOpenChange={setMenuMobile}>
                        <SheetTrigger asChild>
                            <Button variant="ghost" size="icon" className="md:hidden" aria-label="Menu">
                                <Menu />
                            </Button>
                        </SheetTrigger>
                        <SheetContent side="left" className="w-64 p-3">
                            <SheetTitle className="sr-only">Menu</SheetTitle>
                            <Marque />
                            <Menu_ surClic={() => setMenuMobile(false)} />
                        </SheetContent>
                    </Sheet>

                    <div className="flex-1" />

                    <Button
                        variant="ghost"
                        size="icon"
                        aria-label="Changer de thème"
                        onClick={() => setTheme(resolvedTheme === 'dark' ? 'light' : 'dark')}
                    >
                        {resolvedTheme === 'dark' ? <Sun /> : <Moon />}
                    </Button>

                    <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                            <Button variant="ghost" className="gap-2 px-2">
                                <Avatar className="size-7"><AvatarFallback className="text-xs">{initiales}</AvatarFallback></Avatar>
                                <span className="hidden text-sm sm:inline">{session?.prenom} {session?.nom}</span>
                            </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align="end" className="w-56">
                            <DropdownMenuLabel className="font-normal">
                                <div className="text-sm font-medium">{session?.prenom} {session?.nom}</div>
                                <div className="text-muted-foreground text-xs">{session?.email}</div>
                                <div className="text-muted-foreground mt-1 text-xs">{session?.roles.join(', ')}</div>
                            </DropdownMenuLabel>
                            <DropdownMenuSeparator />
                            <DropdownMenuItem onClick={deconnecter}>
                                <LogOut /> Se déconnecter
                            </DropdownMenuItem>
                        </DropdownMenuContent>
                    </DropdownMenu>
                </header>

                <main className="mx-auto w-full max-w-7xl flex-1 p-4 md:p-6">
                    <Outlet />
                </main>
            </div>
        </div>
    );
}
