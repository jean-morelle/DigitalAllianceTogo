import { useState } from 'react';
import { Link, useSearchParams } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { Plus, Search } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Chargement, EnTetePage, EtatErreur, EtatVide, StatutBadge } from '@/components/commun';
import { Pagination } from '@/components/Pagination';
import { api } from '@/lib/api';
import { formatDate, formatFcfa, libelle } from '@/lib/format';
import type { Devis, PaginatedList } from '@/lib/types';

const STATUTS = ['Brouillon', 'ValidationInterne', 'Envoye', 'ModificationDemandee', 'Accepte', 'Refuse', 'Expire'];
const TOUS = 'tous';

export function DevisPage() {
    const [params, setParams] = useSearchParams();
    const statut = params.get('statut') ?? TOUS;
    const page = Number(params.get('page') ?? 1);
    const [recherche, setRecherche] = useState(params.get('q') ?? '');

    const { data, isPending, error } = useQuery({
        queryKey: ['devis', statut, params.get('q'), page],
        queryFn: () => api.get<PaginatedList<Devis>>('/devis', {
            statut: statut === TOUS ? undefined : statut,
            recherche: params.get('q'),
            pageNumber: page,
        }),
    });

    const maj = (changements: Record<string, string | null>) => {
        const suivants = new URLSearchParams(params);
        for (const [cle, valeur] of Object.entries(changements)) {
            if (!valeur || valeur === TOUS) suivants.delete(cle);
            else suivants.set(cle, valeur);
        }
        if (!('page' in changements)) suivants.delete('page');
        setParams(suivants);
    };

    return (
        <>
            <EnTetePage
                titre="Devis"
                description="Brouillon → validation (Admin au-delà du seuil de remise) → envoi → acceptation, qui crée la commande."
                actions={<Button asChild><Link to="/devis/nouveau"><Plus /> Nouveau devis</Link></Button>}
            />

            <div className="mb-4 flex flex-col gap-2 sm:flex-row">
                <form className="relative flex-1" onSubmit={e => { e.preventDefault(); maj({ q: recherche.trim() }); }}>
                    <Search className="text-muted-foreground absolute top-1/2 left-3 size-4 -translate-y-1/2" />
                    <Input className="pl-9" placeholder="Référence du devis ou code client, puis Entrée" value={recherche} onChange={e => setRecherche(e.target.value)} />
                </form>
                <Select value={statut} onValueChange={v => maj({ statut: v })}>
                    <SelectTrigger className="sm:w-60"><SelectValue /></SelectTrigger>
                    <SelectContent>
                        <SelectItem value={TOUS}>Tous les statuts</SelectItem>
                        {STATUTS.map(s => <SelectItem key={s} value={s}>{libelle(s)}</SelectItem>)}
                    </SelectContent>
                </Select>
            </div>

            <Card>
                <CardContent className="p-0">
                    {isPending ? <div className="p-4"><Chargement /></div> : error ? <div className="p-4"><EtatErreur erreur={error} /></div>
                        : data.items.length === 0 ? <EtatVide message="Aucun devis." /> : (
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    <TableHead>Devis</TableHead>
                                    <TableHead>Client</TableHead>
                                    <TableHead>Valable jusqu'au</TableHead>
                                    <TableHead className="text-right">Total</TableHead>
                                    <TableHead>Statut</TableHead>
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                {data.items.map(d => (
                                    <TableRow key={d.id}>
                                        <TableCell>
                                            <Link to={`/devis/${d.id}`} className="text-primary font-medium hover:underline">{d.reference}</Link>
                                            <div className="text-muted-foreground text-xs">{formatDate(d.dateCreation)}</div>
                                        </TableCell>
                                        <TableCell className="font-mono text-xs">{d.codeClient}</TableCell>
                                        <TableCell>{d.statut === 'Envoye' ? formatDate(d.dateValidite, false) : '—'}</TableCell>
                                        <TableCell className="text-right tabular-nums">
                                            {formatFcfa(d.total)}
                                            {d.remise > 0 && <div className="text-muted-foreground text-xs">remise {formatFcfa(d.remise)}</div>}
                                        </TableCell>
                                        <TableCell>
                                            <StatutBadge statut={d.statut} />
                                            {d.statut === 'Brouillon' && d.valideParEntreprise && <div className="mt-1 text-xs text-emerald-700">validé, à envoyer</div>}
                                        </TableCell>
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
