import { useState } from 'react';
import { Link, useSearchParams } from 'react-router';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Ban, CheckCircle2, Send, ShieldCheck, XCircle } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Chargement, EnTetePage, EtatErreur, EtatVide, StatutBadge } from '@/components/commun';
import { DialogueAction } from '@/components/DialogueAction';
import { Pagination } from '@/components/Pagination';
import { api } from '@/lib/api';
import { useAuth } from '@/lib/auth';
import { depuis, formatDate, formatFcfa } from '@/lib/format';
import type { Avoir, PaginatedList, Remboursement } from '@/lib/types';

const TOUS = 'tous';

function useAction() {
    const queryClient = useQueryClient();
    return (promesse: Promise<unknown>, succes: string) => promesse
        .then(() => {
            toast.success(succes);
            for (const cle of ['remboursements', 'avoirs', 'a-traiter', 'commande', 'commandes', 'ticket-sav', 'sav']) void queryClient.invalidateQueries({ queryKey: [cle] });
        })
        .catch((e: Error) => { toast.error(e.message); throw e; });
}

function Filtres({ valeur, surChangement, options }: { valeur: string; surChangement: (v: string) => void; options: [string, string][] }) {
    return (
        <Tabs value={valeur} onValueChange={surChangement} className="mb-4">
            <TabsList>
                {options.map(([v, l]) => <TabsTrigger key={v} value={v}>{l}</TabsTrigger>)}
            </TabsList>
        </Tabs>
    );
}

/** Exécution d'un remboursement : l'argent est-il parti ? */
function DialogueExecuter({ r }: { r: Remboursement }) {
    const agir = useAction();
    const [reference, setReference] = useState('');
    return (
        <>
            <DialogueAction
                declencheur={<Button size="sm"><Send /> {r.statut === 'Echoue' ? 'Réessayer' : 'Exécuté'}</Button>}
                titre={`Remboursement de ${formatFcfa(r.montant)} effectué`}
                description="Indiquez la référence du transfert (T-Money, Flooz, virement) comme preuve."
                libelleConfirmer="Confirmer l'exécution"
                onConfirmer={() => {
                    if (!reference.trim()) { toast.error('La référence du transfert est obligatoire.'); return Promise.reject(new Error('Référence manquante')); }
                    return agir(api.post(`/remboursements/${r.id}/executer`, { reussi: true, referenceTransaction: reference.trim() }), 'Remboursement exécuté.');
                }}
            >
                <div className="grid gap-2">
                    <Label htmlFor={`tx-${r.id}`}>Référence du transfert *</Label>
                    <Input id={`tx-${r.id}`} value={reference} onChange={e => setReference(e.target.value)} maxLength={100} />
                </div>
            </DialogueAction>
            <DialogueAction
                declencheur={<Button size="sm" variant="outline"><XCircle /> Échec</Button>}
                titre="Le remboursement a échoué"
                description="La commande reste en attente de régularisation ; le Commercial prévient le client."
                champMotif="Pourquoi ? (numéro invalide, compte fermé...)" motifObligatoire destructif
                libelleConfirmer="Enregistrer l'échec"
                onConfirmer={motif => agir(api.post(`/remboursements/${r.id}/executer`, { reussi: false, motifEchec: motif }), 'Échec enregistré.')}
            />
        </>
    );
}

function Remboursements() {
    const { aRole } = useAuth();
    const agir = useAction();
    const [statut, setStatut] = useState('EnAttente');
    const [page, setPage] = useState(1);
    const { data, isPending, error } = useQuery({
        queryKey: ['remboursements', statut, page],
        queryFn: () => api.get<PaginatedList<Remboursement>>('/remboursements', { statut: statut === TOUS ? undefined : statut, pageNumber: page }),
    });

    return (
        <>
            <Filtres valeur={statut} surChangement={v => { setStatut(v); setPage(1); }}
                options={[['EnAttente', 'À valider'], ['Valide', 'À exécuter'], ['Echoue', 'Échoués'], ['Execute', 'Exécutés'], [TOUS, 'Tous']]} />
            <Card>
                <CardContent className="p-0">
                    {isPending ? <div className="p-4"><Chargement /></div> : error ? <div className="p-4"><EtatErreur erreur={error} /></div>
                        : data.items.length === 0 ? <EtatVide message="Aucun remboursement." /> : (
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    <TableHead>Remboursement</TableHead>
                                    <TableHead>Commande</TableHead>
                                    <TableHead>Motif</TableHead>
                                    <TableHead className="text-right">Montant</TableHead>
                                    <TableHead>Statut</TableHead>
                                    {aRole() && <TableHead className="text-right">Actions</TableHead>}
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                {data.items.map(r => (
                                    <TableRow key={r.id}>
                                        <TableCell>
                                            <div className="font-medium">{r.reference}</div>
                                            <div className="text-muted-foreground text-xs" title={formatDate(r.dateDemande)}>demandé {depuis(r.dateDemande)}</div>
                                        </TableCell>
                                        <TableCell>
                                            <Link className="text-primary hover:underline" to={`/commandes/${r.commandeId}`}>{r.commandeReference}</Link>
                                            <div className="text-muted-foreground text-xs">version {r.numeroVersion}</div>
                                        </TableCell>
                                        <TableCell className="max-w-xs"><div className="truncate" title={r.motif}>{r.motif}</div></TableCell>
                                        <TableCell className="text-right font-medium tabular-nums">{formatFcfa(r.montant)}</TableCell>
                                        <TableCell>
                                            <StatutBadge statut={r.statut} />
                                            {r.motifEchec && <div className="mt-1 text-xs text-red-600">{r.motifEchec}</div>}
                                            {r.referenceTransaction && <div className="text-muted-foreground mt-1 font-mono text-xs">{r.referenceTransaction}</div>}
                                        </TableCell>
                                        {aRole() && (
                                            <TableCell>
                                                <div className="flex justify-end gap-2">
                                                    {r.statut === 'EnAttente' && (
                                                        <DialogueAction declencheur={<Button size="sm"><ShieldCheck /> Valider</Button>}
                                                            titre={`Valider le remboursement de ${formatFcfa(r.montant)}`} description={r.motif}
                                                            libelleConfirmer="Valider" onConfirmer={() => agir(api.post(`/remboursements/${r.id}/valider`), 'Remboursement validé : à exécuter.')} />
                                                    )}
                                                    {(r.statut === 'Valide' || r.statut === 'Echoue') && <DialogueExecuter r={r} />}
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

function Avoirs() {
    const { aRole } = useAuth();
    const agir = useAction();
    const [statut, setStatut] = useState('EnAttente');
    const [page, setPage] = useState(1);
    const { data, isPending, error } = useQuery({
        queryKey: ['avoirs', statut, page],
        queryFn: () => api.get<PaginatedList<Avoir>>('/avoirs', { statut: statut === TOUS ? undefined : statut, pageNumber: page }),
    });

    return (
        <>
            <Filtres valeur={statut} surChangement={v => { setStatut(v); setPage(1); }}
                options={[['EnAttente', 'À valider'], ['Disponible', 'Disponibles'], ['Utilise', 'Utilisés'], ['Annule', 'Annulés'], [TOUS, 'Tous']]} />
            <Card>
                <CardContent className="p-0">
                    {isPending ? <div className="p-4"><Chargement /></div> : error ? <div className="p-4"><EtatErreur erreur={error} /></div>
                        : data.items.length === 0 ? <EtatVide message="Aucun avoir." /> : (
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    <TableHead>Avoir</TableHead>
                                    <TableHead>Commande d'origine</TableHead>
                                    <TableHead>Motif</TableHead>
                                    <TableHead className="text-right">Montant</TableHead>
                                    <TableHead className="text-right">Solde</TableHead>
                                    <TableHead>Statut</TableHead>
                                    {aRole() && <TableHead className="text-right">Actions</TableHead>}
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                {data.items.map(a => (
                                    <TableRow key={a.id}>
                                        <TableCell>
                                            <div className="font-medium">{a.reference}</div>
                                            <div className="text-muted-foreground text-xs">{formatDate(a.dateCreation, false)}</div>
                                        </TableCell>
                                        <TableCell><Link className="text-primary hover:underline" to={`/commandes/${a.commandeId}`}>{a.commandeReference}</Link></TableCell>
                                        <TableCell className="max-w-xs"><div className="truncate" title={a.motif}>{a.motif}</div></TableCell>
                                        <TableCell className="text-right tabular-nums">{formatFcfa(a.montant)}</TableCell>
                                        <TableCell className="text-right font-medium tabular-nums">{formatFcfa(a.montantRestant)}</TableCell>
                                        <TableCell><StatutBadge statut={a.statut} /></TableCell>
                                        {aRole() && (
                                            <TableCell>
                                                <div className="flex justify-end gap-2">
                                                    {a.statut === 'EnAttente' && (
                                                        <DialogueAction declencheur={<Button size="sm"><CheckCircle2 /> Valider</Button>}
                                                            titre={`Valider l'avoir de ${formatFcfa(a.montant)}`} description="Il sera aussitôt utilisable par le client pour payer une commande."
                                                            libelleConfirmer="Valider" onConfirmer={() => agir(api.post(`/avoirs/${a.id}/valider`), 'Avoir disponible pour le client.')} />
                                                    )}
                                                    {['EnAttente', 'Valide', 'Disponible'].includes(a.statut) && a.montantUtilise === 0 && (
                                                        <DialogueAction declencheur={<Button size="sm" variant="outline"><Ban /> Annuler</Button>}
                                                            titre="Annuler l'avoir" champMotif="Motif" motifObligatoire destructif libelleConfirmer="Annuler l'avoir"
                                                            onConfirmer={motif => agir(api.post(`/avoirs/${a.id}/annuler`, { motif }), 'Avoir annulé.')} />
                                                    )}
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

/** Régularisation financière (§22-23) : l'Administrateur valide et exécute, le Commercial suit. */
export function FinancePage() {
    const { aRole } = useAuth();
    const [params, setParams] = useSearchParams();
    const onglet = params.get('onglet') === 'avoirs' ? 'avoirs' : 'remboursements';
    return (
        <>
            <EnTetePage
                titre="Remboursements & avoirs"
                description={aRole()
                    ? 'Validez chaque demande, puis confirmez l\'envoi de l\'argent avec la référence du transfert.'
                    : 'Suivi des régularisations : seul l\'Administrateur valide et exécute.'}
            />
            <Tabs value={onglet} onValueChange={v => setParams(v === 'avoirs' ? { onglet: 'avoirs' } : {})} className="mb-2">
                <TabsList>
                    <TabsTrigger value="remboursements">Remboursements</TabsTrigger>
                    <TabsTrigger value="avoirs">Avoirs</TabsTrigger>
                </TabsList>
            </Tabs>
            {onglet === 'avoirs' ? <Avoirs /> : <Remboursements />}
        </>
    );
}
