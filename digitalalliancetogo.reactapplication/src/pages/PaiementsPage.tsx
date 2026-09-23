import { useState } from 'react';
import { Link } from 'react-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Check, X } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Chargement, EnTetePage, EtatErreur, EtatVide, StatutBadge } from '@/components/commun';
import { DialogueAction } from '@/components/DialogueAction';
import { LienPreuve } from '@/components/Fichiers';
import { Pagination } from '@/components/Pagination';
import { api } from '@/lib/api';
import { depuis, formatDate, formatFcfa, libelle } from '@/lib/format';
import type { PaginatedList, Paiement } from '@/lib/types';

interface ResultatConfirmation {
    statutCommande: string;
    stockReserve: boolean;
    message: string;
}

export function PaiementsPage() {
    const [statut, setStatut] = useState('EnAttente');
    const [page, setPage] = useState(1);
    const queryClient = useQueryClient();

    const { data, isPending, error } = useQuery({
        queryKey: ['paiements', statut, page],
        queryFn: () => api.get<PaginatedList<Paiement>>('/paiements', { statut, pageNumber: page }),
    });

    const rafraichir = () => {
        void queryClient.invalidateQueries({ queryKey: ['paiements'] });
        void queryClient.invalidateQueries({ queryKey: ['a-traiter'] });
        void queryClient.invalidateQueries({ queryKey: ['commandes'] });
    };

    const confirmer = useMutation({
        mutationFn: (id: string) => api.post<ResultatConfirmation>(`/paiements/${id}/confirmer`),
        onSuccess: r => {
            (r.stockReserve ? toast.success : toast.warning)(r.message);
            rafraichir();
        },
        onError: e => toast.error(e.message),
    });

    const rejeter = useMutation({
        mutationFn: ({ id, motif }: { id: string; motif: string }) => api.post(`/paiements/${id}/rejeter`, { motif }),
        onSuccess: () => {
            toast.success('Paiement rejeté : le client peut soumettre une nouvelle preuve.');
            rafraichir();
        },
        onError: e => toast.error(e.message),
    });

    return (
        <>
            <EnTetePage
                titre="Paiements"
                description="Vérifiez chaque transaction (T-Money, Flooz, virement) avant de la confirmer : la confirmation réserve le stock."
            />

            <Tabs value={statut} onValueChange={v => { setStatut(v); setPage(1); }} className="mb-4">
                <TabsList>
                    <TabsTrigger value="EnAttente">À vérifier</TabsTrigger>
                    <TabsTrigger value="Confirme">Confirmés</TabsTrigger>
                    <TabsTrigger value="Echoue">Rejetés</TabsTrigger>
                </TabsList>
            </Tabs>

            <Card>
                <CardContent className="p-0">
                    {isPending ? <div className="p-4"><Chargement /></div> : error ? <div className="p-4"><EtatErreur erreur={error} /></div>
                        : data.items.length === 0 ? <EtatVide message={statut === 'EnAttente' ? 'Aucune preuve de paiement à vérifier.' : 'Aucun paiement.'} /> : (
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    <TableHead>Paiement</TableHead>
                                    <TableHead>Commande</TableHead>
                                    <TableHead>Transaction</TableHead>
                                    <TableHead className="text-right">Montant</TableHead>
                                    <TableHead>Statut</TableHead>
                                    {statut === 'EnAttente' && <TableHead className="text-right">Vérification</TableHead>}
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                {data.items.map(p => (
                                    <TableRow key={p.id}>
                                        <TableCell>
                                            <div className="font-medium">{p.reference}</div>
                                            <div className="text-muted-foreground text-xs" title={formatDate(p.datePaiement)}>{depuis(p.datePaiement)}</div>
                                        </TableCell>
                                        <TableCell>
                                            <Link to={`/commandes/${p.commandeId}`} className="text-primary hover:underline">{p.commandeReference}</Link>
                                            <div className="text-muted-foreground text-xs">version {p.numeroVersion}</div>
                                        </TableCell>
                                        <TableCell>
                                            <div className="font-mono text-sm">{p.referenceExterne ?? '—'}</div>
                                            <div className="text-muted-foreground flex items-center gap-2 text-xs">
                                                {libelle(p.mode)}
                                                {p.preuveUrl && <LienPreuve url={p.preuveUrl} libelle="preuve" />}
                                            </div>
                                            {p.motifRejet && <div className="text-xs text-red-600">{p.motifRejet}</div>}
                                        </TableCell>
                                        <TableCell className="text-right font-medium tabular-nums">{formatFcfa(p.montant)}</TableCell>
                                        <TableCell><StatutBadge statut={p.statut} /></TableCell>
                                        {statut === 'EnAttente' && (
                                            <TableCell className="text-right">
                                                <div className="flex justify-end gap-2">
                                                    <DialogueAction
                                                        declencheur={<Button size="sm" variant="outline"><X /> Rejeter</Button>}
                                                        titre={`Rejeter ${p.reference}`}
                                                        description="Le client sera invité à soumettre une nouvelle preuve dans le délai autorisé."
                                                        champMotif="Motif (visible par le client)"
                                                        motifObligatoire
                                                        libelleConfirmer="Rejeter"
                                                        destructif
                                                        onConfirmer={motif => rejeter.mutateAsync({ id: p.id, motif })}
                                                    />
                                                    <DialogueAction
                                                        declencheur={<Button size="sm"><Check /> Confirmer</Button>}
                                                        titre={`Confirmer ${formatFcfa(p.montant)} ?`}
                                                        description={`Vous attestez avoir vérifié la transaction ${p.referenceExterne ?? ''}. Le stock de la commande ${p.commandeReference} sera réservé.`}
                                                        libelleConfirmer="Confirmer le paiement"
                                                        onConfirmer={() => confirmer.mutateAsync(p.id)}
                                                    />
                                                </div>
                                            </TableCell>
                                        )}
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                    )}
                </CardContent>
            </Card>
            {data && <Pagination page={page} totalPages={data.totalPages} surChangement={setPage} />}
        </>
    );
}
