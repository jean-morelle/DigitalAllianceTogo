import { Link, useSearchParams } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { Card, CardContent } from '@/components/ui/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Chargement, EnTetePage, EtatErreur, EtatVide, StatutBadge } from '@/components/commun';
import { Pagination } from '@/components/Pagination';
import { api } from '@/lib/api';
import { useAuth } from '@/lib/auth';
import { depuis, formatDate, libelle } from '@/lib/format';
import { Roles } from '@/lib/roles';
import type { PaginatedList, TicketSav } from '@/lib/types';
import { DialogueNouveauTicket } from './DialogueNouveauTicket';

const STATUTS = ['Ouvert', 'EnReparation', 'DecisionCommerciale', 'RemplacementEnCours', 'EnAttenteRegulationFinanciere', 'Cloture'];
const TOUS = 'tous';

export function SavPage() {
    const { aRole } = useAuth();
    const [params, setParams] = useSearchParams();
    const statut = params.get('statut') ?? TOUS;
    const page = Number(params.get('page') ?? 1);

    const { data, isPending, error } = useQuery({
        queryKey: ['sav', statut, page],
        queryFn: () => api.get<PaginatedList<TicketSav>>('/sav', { statut: statut === TOUS ? undefined : statut, pageNumber: page }),
    });

    const maj = (cle: string, valeur: string) => {
        const suivants = new URLSearchParams(params);
        if (valeur === TOUS) suivants.delete(cle); else suivants.set(cle, valeur);
        if (cle !== 'page') suivants.delete('page');
        setParams(suivants);
    };

    return (
        <>
            <EnTetePage
                titre="Service après-vente"
                description="Diagnostic par le technicien → réparation, ou décision commerciale (remplacement, remboursement, avoir)."
                actions={aRole(Roles.Commercial) ? <DialogueNouveauTicket /> : undefined}
            />

            <Select value={statut} onValueChange={v => maj('statut', v)}>
                <SelectTrigger className="mb-4 sm:w-72"><SelectValue /></SelectTrigger>
                <SelectContent>
                    <SelectItem value={TOUS}>Tous les tickets</SelectItem>
                    {STATUTS.map(s => <SelectItem key={s} value={s}>{libelle(s)}</SelectItem>)}
                </SelectContent>
            </Select>

            <Card>
                <CardContent className="p-0">
                    {isPending ? <div className="p-4"><Chargement /></div> : error ? <div className="p-4"><EtatErreur erreur={error} /></div>
                        : data.items.length === 0 ? <EtatVide message="Aucun ticket." /> : (
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    <TableHead>Ticket</TableHead>
                                    <TableHead>Produit</TableHead>
                                    <TableHead>Commande</TableHead>
                                    <TableHead>Problème</TableHead>
                                    <TableHead>Statut</TableHead>
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                {data.items.map(t => (
                                    <TableRow key={t.id}>
                                        <TableCell>
                                            <Link to={`/sav/${t.id}`} className="text-primary font-medium hover:underline">{t.reference}</Link>
                                            <div className="text-muted-foreground text-xs" title={formatDate(t.dateCreation)}>{depuis(t.dateCreation)}</div>
                                        </TableCell>
                                        <TableCell>{t.produitNom}{t.quantite > 1 && <span className="text-muted-foreground"> × {t.quantite}</span>}</TableCell>
                                        <TableCell>
                                            <div>{t.commandeReference}</div>
                                            <div className="text-muted-foreground font-mono text-xs">{t.codeClient}</div>
                                        </TableCell>
                                        <TableCell className="max-w-xs truncate">{t.motif}</TableCell>
                                        <TableCell>
                                            <StatutBadge statut={t.statut} />
                                            {t.resolution && <div className="text-muted-foreground mt-1 text-xs">{t.resolution}</div>}
                                        </TableCell>
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                    )}
                </CardContent>
            </Card>
            {data && <Pagination page={page} totalPages={data.totalPages} surChangement={p => maj('page', String(p))} />}
        </>
    );
}
