import { useState } from 'react';
import { Link, useSearchParams } from 'react-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { AlertTriangle, BellRing, History, X } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle, SheetTrigger } from '@/components/ui/sheet';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Chargement, EtatErreur, EtatVide, StatutBadge } from '@/components/commun';
import { DialogueAction } from '@/components/DialogueAction';
import { SelecteurProduit } from '@/components/Selecteurs';
import { api } from '@/lib/api';
import { formatDate } from '@/lib/format';
import type { Entrepot, MouvementStock, PaginatedList, StockProduit } from '@/lib/types';
import { cn } from '@/lib/utils';

const TOUS = 'tous';

/** Historique des mouvements d'une ligne de stock, dans un panneau latéral. */
function Mouvements({ stock }: { stock: StockProduit }) {
    const [ouvert, setOuvert] = useState(false);
    const { data, isPending, error } = useQuery({
        queryKey: ['mouvements', stock.id],
        queryFn: () => api.get<PaginatedList<MouvementStock>>(`/stocks/${stock.id}/mouvements`),
        enabled: ouvert,
    });

    return (
        <Sheet open={ouvert} onOpenChange={setOuvert}>
            <SheetTrigger asChild>
                <Button variant="ghost" size="icon" aria-label="Historique des mouvements"><History /></Button>
            </SheetTrigger>
            <SheetContent className="w-full overflow-y-auto sm:max-w-lg">
                <SheetHeader>
                    <SheetTitle>{stock.produitNom}</SheetTitle>
                    <SheetDescription>{stock.entrepotNom} · mouvements du plus récent au plus ancien</SheetDescription>
                </SheetHeader>
                <div className="px-4 pb-4">
                    {isPending ? <Chargement /> : error ? <EtatErreur erreur={error} /> : data.items.length === 0 ? <EtatVide message="Aucun mouvement." /> : (
                        <ol className="space-y-3">
                            {data.items.map(m => (
                                <li key={m.id} className="rounded-md border p-3 text-sm">
                                    <div className="flex items-center justify-between gap-2">
                                        <StatutBadge statut={m.type} />
                                        <span className="font-semibold tabular-nums">{m.quantite}</span>
                                    </div>
                                    <div className="mt-1">{m.motif}</div>
                                    <div className="text-muted-foreground mt-1 text-xs">
                                        {formatDate(m.dateMouvement)} · {m.reference}
                                        {m.commandeId && <> · <Link className="text-primary hover:underline" to={`/commandes/${m.commandeId}`}>{m.commandeReference}</Link></>}
                                        {m.ticketSAVId && ' · SAV'}
                                    </div>
                                </li>
                            ))}
                        </ol>
                    )}
                </div>
            </SheetContent>
        </Sheet>
    );
}

function DialogueSeuil({ stock }: { stock: StockProduit }) {
    const queryClient = useQueryClient();
    const [seuil, setSeuil] = useState(stock.seuilAlerte);
    const modifier = useMutation({
        mutationFn: () => api.put(`/stocks/${stock.id}/seuil-alerte`, { seuilAlerte: seuil }),
        onSuccess: () => {
            toast.success(seuil === 0 ? 'Alerte désactivée.' : `Alerte à ${seuil} unité(s) disponible(s).`);
            void queryClient.invalidateQueries({ queryKey: ['stocks'] });
            void queryClient.invalidateQueries({ queryKey: ['a-traiter'] });
        },
        onError: e => toast.error(e.message),
    });

    return (
        <DialogueAction
            declencheur={<Button variant="ghost" size="icon" aria-label="Seuil d'alerte"><BellRing /></Button>}
            titre={`Seuil d'alerte — ${stock.produitNom}`}
            description={`${stock.entrepotNom} : le produit apparaîtra « sous le seuil » quand le disponible atteindra ce niveau. 0 = pas d'alerte.`}
            libelleConfirmer="Enregistrer"
            onConfirmer={() => modifier.mutateAsync()}
        >
            <div className="grid gap-2">
                <Label htmlFor="seuil">Seuil (unités disponibles)</Label>
                <Input id="seuil" type="number" min={0} value={seuil} onChange={e => setSeuil(Math.max(0, Math.floor(Number(e.target.value) || 0)))} />
            </div>
        </DialogueAction>
    );
}

export function EtatStock({ gestion }: { gestion: boolean }) {
    const [params, setParams] = useSearchParams();
    const [produit, setProduit] = useState<{ id: string; nom: string } | null>(null);
    const entrepotId = params.get('entrepot') ?? TOUS;
    const sousSeuil = params.get('sousSeuil') === 'true';

    const { data: entrepots } = useQuery({ queryKey: ['entrepots'], queryFn: () => api.get<Entrepot[]>('/stocks/entrepots') });
    const { data, isPending, error } = useQuery({
        queryKey: ['stocks', produit?.id, entrepotId, sousSeuil],
        queryFn: () => api.get<StockProduit[]>('/stocks', {
            produitId: produit?.id,
            entrepotId: entrepotId === TOUS ? undefined : entrepotId,
            sousSeuilSeulement: sousSeuil || undefined,
        }),
    });

    const maj = (cle: string, valeur: string | null) => {
        const suivants = new URLSearchParams(params);
        if (!valeur || valeur === TOUS) suivants.delete(cle); else suivants.set(cle, valeur);
        setParams(suivants);
    };

    return (
        <div className="space-y-4">
            <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
                <div className="flex flex-1 items-center gap-1">
                    <SelecteurProduit libelleSelection={produit?.nom ?? 'Tous les produits'} surChoix={p => setProduit({ id: p.id, nom: p.nom })} />
                    {produit && <Button variant="ghost" size="icon" aria-label="Tous les produits" onClick={() => setProduit(null)}><X /></Button>}
                </div>
                <Select value={entrepotId} onValueChange={v => maj('entrepot', v)}>
                    <SelectTrigger className="sm:w-56"><SelectValue /></SelectTrigger>
                    <SelectContent>
                        <SelectItem value={TOUS}>Tous les entrepôts</SelectItem>
                        {entrepots?.map(e => <SelectItem key={e.id} value={e.id}>{e.nom}</SelectItem>)}
                    </SelectContent>
                </Select>
                <Button variant={sousSeuil ? 'default' : 'outline'} onClick={() => maj('sousSeuil', sousSeuil ? null : 'true')}>
                    <AlertTriangle /> Sous le seuil
                </Button>
            </div>

            <Card>
                <CardContent className="p-0">
                    {isPending ? <div className="p-4"><Chargement /></div> : error ? <div className="p-4"><EtatErreur erreur={error} /></div>
                        : data.length === 0 ? <EtatVide message={sousSeuil ? 'Aucun produit sous le seuil d\'alerte.' : 'Aucun stock.'} /> : (
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    <TableHead>Produit</TableHead>
                                    <TableHead>Entrepôt</TableHead>
                                    <TableHead className="text-right" title="Physique − réservé">Disponible</TableHead>
                                    <TableHead className="text-right">Physique</TableHead>
                                    <TableHead className="text-right">Réservé</TableHead>
                                    <TableHead className="text-right">En transit</TableHead>
                                    <TableHead className="text-right">Défectueux</TableHead>
                                    <TableHead className="text-right">Seuil</TableHead>
                                    <TableHead className="w-20" />
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                {data.map(s => {
                                    const alerte = s.seuilAlerte > 0 && s.quantiteDisponible <= s.seuilAlerte;
                                    return (
                                        <TableRow key={s.id}>
                                            <TableCell><div className="font-medium">{s.produitNom}</div><div className="text-muted-foreground text-xs">{s.produitReference}</div></TableCell>
                                            <TableCell>{s.entrepotNom}</TableCell>
                                            <TableCell className={cn('text-right text-base font-semibold tabular-nums', alerte && 'text-amber-700 dark:text-amber-400')}>
                                                {alerte && <AlertTriangle className="mr-1 inline size-4" />}{s.quantiteDisponible}
                                            </TableCell>
                                            <TableCell className="text-right tabular-nums">{s.quantitePhysique}</TableCell>
                                            <TableCell className="text-right tabular-nums">{s.quantiteReservee || '—'}</TableCell>
                                            <TableCell className="text-right tabular-nums">{s.quantiteEnTransit || '—'}</TableCell>
                                            <TableCell className={cn('text-right tabular-nums', s.quantiteDefectueuse > 0 && 'text-red-600')}>{s.quantiteDefectueuse || '—'}</TableCell>
                                            <TableCell className="text-muted-foreground text-right tabular-nums">{s.seuilAlerte || '—'}</TableCell>
                                            <TableCell>
                                                <div className="flex justify-end">
                                                    {gestion && <DialogueSeuil stock={s} />}
                                                    <Mouvements stock={s} />
                                                </div>
                                            </TableCell>
                                        </TableRow>
                                    );
                                })}
                            </TableBody>
                        </Table>
                    )}
                </CardContent>
            </Card>
            <p className="text-muted-foreground text-xs">
                Disponible = physique − réservé. Les unités en transit (chez le livreur) et défectueuses ne sont pas vendables.
            </p>
        </div>
    );
}
