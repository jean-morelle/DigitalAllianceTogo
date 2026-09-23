import { useState } from 'react';
import { useNavigate } from 'react-router';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Bell, CheckCheck } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { Chargement } from '@/components/commun';
import { api } from '@/lib/api';
import { depuis } from '@/lib/format';
import type { NotificationClient, PaginatedList } from '@/lib/types';
import { cn } from '@/lib/utils';

/** Cloche du client : pastille des non lues (rafraîchie chaque minute), dernières notifications au clic. */
export function Cloche() {
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const [ouvert, setOuvert] = useState(false);

    const { data: nonLues = 0 } = useQuery({
        queryKey: ['notifications', 'non-lues'],
        queryFn: () => api.get<number>('/notifications/non-lues'),
        refetchInterval: 60_000,
    });
    const { data: liste, isPending } = useQuery({
        queryKey: ['notifications', 'liste'],
        queryFn: () => api.get<PaginatedList<NotificationClient>>('/notifications', { pageSize: 15 }),
        enabled: ouvert,
    });

    const rafraichir = () => void queryClient.invalidateQueries({ queryKey: ['notifications'] });

    const ouvrir = (n: NotificationClient) => {
        setOuvert(false);
        if (!n.lue) void api.post(`/notifications/${n.id}/lue`, {}).then(rafraichir);
        if (n.lien) {
            // La page visée affiche l'état à jour (paiement confirmé, livraison...)
            for (const cle of ['mes-commandes', 'ma-commande', 'mes-devis', 'mon-devis', 'mes-tickets', 'mes-avoirs'])
                void queryClient.invalidateQueries({ queryKey: [cle] });
            navigate(n.lien);
        }
    };

    return (
        <Popover open={ouvert} onOpenChange={setOuvert}>
            <PopoverTrigger asChild>
                <Button variant="ghost" size="icon" className="relative" aria-label={nonLues ? `Notifications (${nonLues} non lues)` : 'Notifications'}>
                    <Bell />
                    {nonLues > 0 && (
                        <span className="bg-destructive absolute -top-1 -right-1 flex min-w-5 items-center justify-center rounded-full px-1 text-xs text-white">
                            {nonLues > 9 ? '9+' : nonLues}
                        </span>
                    )}
                </Button>
            </PopoverTrigger>
            <PopoverContent align="end" className="w-[min(24rem,calc(100vw-2rem))] p-0">
                <div className="flex items-center justify-between border-b px-3 py-2">
                    <span className="text-sm font-semibold">Notifications</span>
                    {nonLues > 0 && (
                        <Button variant="ghost" size="sm" className="h-7 text-xs" onClick={() => void api.post('/notifications/tout-lire', {}).then(rafraichir)}>
                            <CheckCheck /> Tout marquer lu
                        </Button>
                    )}
                </div>
                <div className="max-h-[60vh] overflow-y-auto">
                    {isPending ? <div className="p-4"><Chargement /></div>
                        : !liste?.items.length ? <p className="text-muted-foreground p-6 text-center text-sm">Aucune notification pour le moment.</p>
                        : liste.items.map(n => (
                            <button key={n.id} type="button" onClick={() => ouvrir(n)}
                                className={cn('hover:bg-muted flex w-full gap-2 border-b px-3 py-2.5 text-left last:border-b-0', !n.lue && 'bg-primary/5')}>
                                <span className={cn('mt-1.5 size-2 shrink-0 rounded-full', n.lue ? 'bg-transparent' : 'bg-primary')} />
                                <span className="min-w-0 flex-1">
                                    <span className={cn('block text-sm', !n.lue && 'font-semibold')}>{n.titre}</span>
                                    <span className="text-muted-foreground line-clamp-3 block text-xs">{n.message}</span>
                                    <span className="text-muted-foreground mt-0.5 block text-[11px]">{depuis(n.dateCreation)}</span>
                                </span>
                            </button>
                        ))}
                </div>
            </PopoverContent>
        </Popover>
    );
}
