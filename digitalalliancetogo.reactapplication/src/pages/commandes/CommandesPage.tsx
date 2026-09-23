import { useState } from 'react';
import { Link, useSearchParams } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { Search } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Chargement, EnTetePage, EtatErreur, EtatVide, StatutBadge } from '@/components/commun';
import { Pagination } from '@/components/Pagination';
import { api } from '@/lib/api';
import { formatDate, formatFcfa, libelle } from '@/lib/format';
import type { Commande, PaginatedList } from '@/lib/types';

const STATUTS = [
    'CommandeCreee', 'PaiementEnAttente', 'PaiementEchoue', 'PaiementConfirme', 'EnAttenteDisponibilite', 'StockReserve',
    'PreparationEnCours', 'PretePourLivraison', 'EnTransit', 'Livree', 'LivraisonEchoueeRefusClient', 'AnnulationEnCours',
    'Annulee', 'EnAttenteRegulationFinanciere', 'Cloturee',
];
const TOUS = 'tous';

export function CommandesPage() {
    const [params, setParams] = useSearchParams();
    const statut = params.get('statut') ?? TOUS;
    const [recherche, setRecherche] = useState(params.get('q') ?? '');
    const page = Number(params.get('page') ?? 1);

    const { data, isPending, error } = useQuery({
        queryKey: ['commandes', statut, params.get('q'), page],
        queryFn: () => api.get<PaginatedList<Commande>>('/commandes', {
            statut: statut === TOUS ? undefined : statut,
            recherche: params.get('q'),
            pageNumber: page,
        }),
    });

    const maj = (changements: Record<string, string | null>) => {
        const suivants = new URLSearchParams(params);
        for (const [cle, valeur] of Object.entries(changements)) {
            if (valeur === null || valeur === '' || valeur === TOUS) suivants.delete(cle);
            else suivants.set(cle, valeur);
        }
        if (!('page' in changements)) suivants.delete('page');
        setParams(suivants);
    };

    return (
        <>
            <EnTetePage titre="Commandes" description="Suivi de toutes les commandes, du paiement à la clôture." />

            <div className="mb-4 flex flex-col gap-2 sm:flex-row">
                <form className="relative flex-1" onSubmit={e => { e.preventDefault(); maj({ q: recherche.trim() }); }}>
                    <Search className="text-muted-foreground absolute top-1/2 left-3 size-4 -translate-y-1/2" />
                    <Input className="pl-9" placeholder="Référence de commande ou code client, puis Entrée" value={recherche} onChange={e => setRecherche(e.target.value)} />
                </form>
                <Select value={statut} onValueChange={v => maj({ statut: v })}>
                    <SelectTrigger className="sm:w-64"><SelectValue /></SelectTrigger>
                    <SelectContent>
                        <SelectItem value={TOUS}>Tous les statuts</SelectItem>
                        {STATUTS.map(s => <SelectItem key={s} value={s}>{libelle(s)}</SelectItem>)}
                    </SelectContent>
                </Select>
            </div>

            <Card>
                <CardContent className="p-0">
                    {isPending ? <div className="p-4"><Chargement /></div> : error ? <div className="p-4"><EtatErreur erreur={error} /></div>
                        : data.items.length === 0 ? <EtatVide message="Aucune commande." /> : (
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    <TableHead>Commande</TableHead>
                                    <TableHead>Client</TableHead>
                                    <TableHead>Créée le</TableHead>
                                    <TableHead className="text-right">Total</TableHead>
                                    <TableHead>Statut</TableHead>
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                {data.items.map(c => (
                                    <TableRow key={c.id} className="cursor-pointer">
                                        <TableCell>
                                            <Link to={`/commandes/${c.id}`} className="text-primary font-medium hover:underline">{c.reference}</Link>
                                            {c.versionActive > 1 && <span className="text-muted-foreground ml-2 text-xs">v{c.versionActive}</span>}
                                        </TableCell>
                                        <TableCell className="font-mono text-xs">{c.codeClient}</TableCell>
                                        <TableCell>{formatDate(c.dateCreation)}</TableCell>
                                        <TableCell className="text-right tabular-nums">{formatFcfa(c.total)}</TableCell>
                                        <TableCell><StatutBadge statut={c.statut} /></TableCell>
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                    )}
                </CardContent>
            </Card>
            {data && <Pagination page={page} totalPages={data.totalPages} surChangement={p => maj({ page: String(p) })} />}
        </>
    );
}
