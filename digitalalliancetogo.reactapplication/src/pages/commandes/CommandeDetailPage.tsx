import { useState } from 'react';
import { Link, useParams } from 'react-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, Ban, CheckCheck, ClipboardCheck, FilePen, Lock, PackageCheck, PackageOpen, Undo2 } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Chargement, EtatErreur, EtatVide, StatutBadge } from '@/components/commun';
import { DialogueAction } from '@/components/DialogueAction';
import { api } from '@/lib/api';
import { useAuth } from '@/lib/auth';
import { Roles } from '@/lib/roles';
import { formatDate, formatFcfa, libelle } from '@/lib/format';
import type { CommandeDetail, JournalAudit, LigneCommande, Livraison, PaginatedList, VersionCommande } from '@/lib/types';
import { DialoguePlanifier } from '@/pages/livraisons/ActionsLivraison';
import { cn } from '@/lib/utils';

type Mode = 'Remboursement' | 'Avoir';

function ChoixRegularisation({ valeur, surChangement, montant }: { valeur: Mode; surChangement: (m: Mode) => void; montant?: number }) {
    return (
        <div className="grid gap-2">
            <Label>Le client récupère {montant !== undefined ? formatFcfa(montant) : 'son argent'} par</Label>
            <Select value={valeur} onValueChange={v => surChangement(v as Mode)}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                    <SelectItem value="Remboursement">Remboursement (Mobile Money / virement)</SelectItem>
                    <SelectItem value="Avoir">Avoir (crédit sur une prochaine commande)</SelectItem>
                </SelectContent>
            </Select>
            <p className="text-muted-foreground text-xs">L'opération financière sera validée par l'Administrateur.</p>
        </div>
    );
}

function TableLignes({ lignes }: { lignes: LigneCommande[] }) {
    return (
        <Table>
            <TableHeader>
                <TableRow>
                    <TableHead>Produit</TableHead>
                    <TableHead className="text-right">Qté</TableHead>
                    <TableHead className="text-right">Prix unitaire</TableHead>
                    <TableHead className="text-right">Remise</TableHead>
                    <TableHead className="text-right">Total</TableHead>
                </TableRow>
            </TableHeader>
            <TableBody>
                {lignes.map(l => (
                    <TableRow key={l.id}>
                        <TableCell><div className="font-medium">{l.produitNom}</div><div className="text-muted-foreground text-xs">{l.produitReference}</div></TableCell>
                        <TableCell className="text-right tabular-nums">{l.quantite}</TableCell>
                        <TableCell className="text-right tabular-nums">{formatFcfa(l.prixUnitaire)}</TableCell>
                        <TableCell className="text-right tabular-nums">{l.remise ? formatFcfa(l.remise) : '—'}</TableCell>
                        <TableCell className="text-right font-medium tabular-nums">{formatFcfa(l.total)}</TableCell>
                    </TableRow>
                ))}
            </TableBody>
        </Table>
    );
}

/** Dialogue de réception d'un retour : quantité endommagée par produit, le reste est intact. */
function DialogueReceptionRetour({ commande, surValider }: { commande: CommandeDetail; surValider: (defectueux: { produitId: string; quantite: number }[]) => Promise<unknown> }) {
    const [defectueux, setDefectueux] = useState<Record<string, number>>({});
    return (
        <DialogueAction
            declencheur={<Button><PackageCheck /> Réceptionner le retour</Button>}
            titre="Réception et contrôle du retour"
            description="Indiquez les unités endommagées : elles passent en défectueux. Les autres redeviennent vendables."
            libelleConfirmer="Valider le contrôle"
            onConfirmer={() => surValider(
                Object.entries(defectueux).filter(([, q]) => q > 0).map(([produitId, quantite]) => ({ produitId, quantite })),
            )}
        >
            <div className="grid gap-3">
                {commande.lignes.map(l => (
                    <div key={l.id} className="flex items-center justify-between gap-3">
                        <div className="text-sm"><div className="font-medium">{l.produitNom}</div><div className="text-muted-foreground text-xs">{l.quantite} unité(s)</div></div>
                        <div className="flex items-center gap-2">
                            <Label htmlFor={`def-${l.produitId}`} className="text-xs">Endommagées</Label>
                            <Input id={`def-${l.produitId}`} type="number" min={0} max={l.quantite} className="w-20"
                                value={defectueux[l.produitId] ?? 0}
                                onChange={e => setDefectueux(d => ({ ...d, [l.produitId]: Math.min(Number(e.target.value), l.quantite) }))} />
                        </div>
                    </div>
                ))}
            </div>
        </DialogueAction>
    );
}

function PropositionEnCours({ commande, version, surAction }: { commande: CommandeDetail; version: VersionCommande; surAction: () => void }) {
    const { aRole } = useAuth();
    const [mode, setMode] = useState<Mode>('Remboursement');
    const actuelle = commande.versions.find(v => v.active);
    const ecart = version.total - (actuelle?.total ?? 0);
    const executer = (promesse: Promise<unknown>, message: string) =>
        promesse.then(r => { toast.success((r as { message?: string } | undefined)?.message ?? message); surAction(); })
            .catch((e: Error) => { toast.error(e.message); throw e; });

    return (
        <Card className="mb-6 border-amber-300">
            <CardHeader>
                <CardTitle className="flex flex-wrap items-center gap-2 text-base">
                    Modification proposée : version {version.numeroVersion} <StatutBadge statut={version.statut} />
                </CardTitle>
                <CardDescription>
                    {version.motifModification} · {formatFcfa(actuelle?.total)} → <strong>{formatFcfa(version.total)}</strong>{' '}
                    <span className={cn(ecart > 0 ? 'text-amber-700' : ecart < 0 ? 'text-emerald-700' : '')}>
                        ({ecart > 0 ? '+' : ''}{formatFcfa(ecart)})
                    </span>
                </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
                <TableLignes lignes={version.lignes} />
                <div className="flex flex-wrap justify-end gap-2">
                    {aRole(Roles.Commercial) && (
                        <DialogueAction declencheur={<Button variant="outline"><Undo2 /> Retirer</Button>} titre="Retirer la proposition"
                            champMotif="Motif" motifObligatoire libelleConfirmer="Retirer"
                            onConfirmer={motif => executer(api.post(`/commandes/${commande.id}/modifications/retirer`, { motif }), 'Proposition retirée.')} />
                    )}
                    {version.statut === 'EnValidationAdmin' && aRole() && (
                        <>
                            <DialogueAction declencheur={<Button variant="outline">Refuser</Button>} titre="Refuser la modification"
                                champMotif="Motif du refus" motifObligatoire libelleConfirmer="Refuser" destructif
                                onConfirmer={motif => executer(api.post(`/commandes/${commande.id}/modifications/decision-admin`, { valider: false, motif }), 'Modification refusée.')} />
                            <DialogueAction declencheur={<Button>Valider et envoyer au client</Button>} titre="Valider la modification"
                                description="La proposition sera soumise à l'acceptation du client." libelleConfirmer="Valider"
                                onConfirmer={() => executer(api.post(`/commandes/${commande.id}/modifications/decision-admin`, { valider: true }), 'Proposition envoyée au client.')} />
                        </>
                    )}
                    {version.statut === 'EnAttenteClient' && aRole(Roles.Commercial) && (
                        <>
                            <DialogueAction declencheur={<Button variant="outline">Refus du client</Button>} titre="Le client refuse la modification"
                                champMotif="Commentaire" libelleConfirmer="Enregistrer le refus"
                                onConfirmer={motif => executer(api.post(`/commandes/${commande.id}/modifications/reponse`, { accepter: false, motif }), 'Refus enregistré.')} />
                            <DialogueAction declencheur={<Button><CheckCheck /> Acceptation du client</Button>} titre="Le client accepte la version"
                                description={ecart > 0 ? `Un complément sera à payer avant de poursuivre.` : undefined}
                                libelleConfirmer="Enregistrer l'acceptation"
                                onConfirmer={() => executer(api.post(`/commandes/${commande.id}/modifications/reponse`, { accepter: true, regularisation: mode }), 'Version acceptée.')}>
                                {ecart < 0 && <ChoixRegularisation valeur={mode} surChangement={setMode} />}
                            </DialogueAction>
                        </>
                    )}
                </div>
            </CardContent>
        </Card>
    );
}

function Historique({ commandeId }: { commandeId: string }) {
    const { data, isPending, error } = useQuery({
        queryKey: ['historique', commandeId],
        queryFn: () => api.get<JournalAudit[]>(`/audit/commandes/${commandeId}`),
    });
    if (isPending) return <Chargement />;
    if (error) return <EtatErreur erreur={error} />;
    if (data.length === 0) return <EtatVide message="Aucun événement." />;
    return (
        <ol className="relative ml-2 border-l pl-6">
            {data.map(j => (
                <li key={j.id} className="mb-4">
                    <span className="bg-primary absolute -left-1.5 mt-1.5 size-3 rounded-full" />
                    <div className="text-sm font-medium">{j.action.replace(/([a-z])([A-Z])/g, '$1 $2')}</div>
                    <div className="text-muted-foreground text-xs">{formatDate(j.dateAction)} · {j.auteur} · {j.entite}</div>
                </li>
            ))}
        </ol>
    );
}

export function CommandeDetailPage() {
    const { id = '' } = useParams();
    const { aRole } = useAuth();
    const queryClient = useQueryClient();
    const [modeAnnulation, setModeAnnulation] = useState<Mode>('Remboursement');

    const { data: c, isPending, error } = useQuery({
        queryKey: ['commande', id],
        queryFn: () => api.get<CommandeDetail>(`/commandes/${id}`),
    });
    // Livraisons de la commande : réservé au stock (le Commercial n'y a pas accès côté API)
    const { data: livraisons } = useQuery({
        queryKey: ['livraisons', 'commande', id],
        queryFn: () => api.get<PaginatedList<Livraison>>('/livraisons', { commandeId: id, pageSize: 50 }),
        enabled: aRole(Roles.GestionnaireStock),
    });

    const rafraichir = () => {
        for (const cle of ['commande', 'commandes', 'a-traiter', 'historique']) void queryClient.invalidateQueries({ queryKey: [cle] });
    };
    const action = useMutation({
        mutationFn: ({ chemin, corps }: { chemin: string; corps?: unknown; succes: string }) => api.post<{ message?: string }>(`/commandes/${id}/${chemin}`, corps),
        onSuccess: (r, v) => { toast.success(r?.message ?? v.succes); rafraichir(); },
        onError: e => toast.error(e.message),
    });
    const agir = (chemin: string, succes: string, corps?: unknown) => action.mutateAsync({ chemin, corps, succes });

    if (isPending) return <Chargement lignes={6} />;
    if (error) return <EtatErreur erreur={error} />;

    const s = c.statut;
    const proposition = c.versions.find(v => v.statut === 'EnValidationAdmin' || v.statut === 'EnAttenteClient');
    const payee = ['PaiementConfirme', 'EnAttenteDisponibilite', 'StockReserve', 'PreparationEnCours', 'PretePourLivraison'].includes(s)
        || (['CommandeCreee', 'PaiementEchoue'].includes(s) && c.paiements.some(p => p.statut === 'Confirme'));
    const livraisonEnCours = livraisons?.items.some(l => l.ticketSAVId === null && ['Planifiee', 'EnTransit'].includes(l.statut)) ?? false;
    const dejaTentee = livraisons?.items.some(l => l.ticketSAVId === null && l.statut === 'AReprogrammer') ?? false;
    const annulable = ['CommandeCreee', 'PaiementEchoue', 'PaiementConfirme', 'EnAttenteDisponibilite', 'StockReserve', 'PreparationEnCours', 'PretePourLivraison'].includes(s);

    return (
        <>
            <Button asChild variant="ghost" size="sm" className="mb-2 -ml-2">
                <Link to="/commandes"><ArrowLeft /> Commandes</Link>
            </Button>

            <div className="mb-6 flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                <div>
                    <h1 className="flex flex-wrap items-center gap-3 text-2xl font-semibold">
                        {c.reference} <StatutBadge statut={s} />
                    </h1>
                    <p className="text-muted-foreground mt-1 text-sm">
                        Client <span className="font-mono">{c.codeClient}</span> · créée le {formatDate(c.dateCreation)} · version {c.versionActive}
                    </p>
                </div>

                <div className="flex flex-wrap gap-2">
                    {aRole(Roles.Commercial) && !proposition && ['PaiementConfirme', 'EnAttenteDisponibilite', 'StockReserve', 'PreparationEnCours', 'PretePourLivraison'].includes(s) && (
                        <Button variant="outline" asChild><Link to={`/commandes/${c.id}/modifier`}><FilePen /> Proposer une modification</Link></Button>
                    )}
                    {aRole(Roles.GestionnaireStock) && s === 'PretePourLivraison' && livraisons && !livraisonEnCours && (
                        <DialoguePlanifier commandeId={c.id} relivraison={dejaTentee} />
                    )}
                    {aRole(Roles.GestionnaireStock) && s === 'StockReserve' && (
                        <Button onClick={() => agir('demarrer-preparation', 'Préparation démarrée.')}><PackageOpen /> Démarrer la préparation</Button>
                    )}
                    {aRole(Roles.GestionnaireStock) && s === 'PreparationEnCours' && (
                        <Button onClick={() => agir('terminer-preparation', 'Commande prête pour la livraison.')}><ClipboardCheck /> Colis prêt</Button>
                    )}
                    {aRole(Roles.GestionnaireStock) && (s === 'AnnulationEnCours' || s === 'LivraisonEchoueeRefusClient') && (
                        <DialogueReceptionRetour commande={c} surValider={defectueux => agir('reception-retour', 'Retour contrôlé.', { defectueux })} />
                    )}
                    {aRole(Roles.Commercial) && annulable && (
                        <DialogueAction
                            declencheur={<Button variant="outline"><Ban /> Annuler</Button>}
                            titre={`Annuler ${c.reference}`}
                            description={s === 'PreparationEnCours' || s === 'PretePourLivraison'
                                ? 'La commande est en préparation : le stock devra récupérer et contrôler les produits.'
                                : payee ? 'La réservation sera libérée.' : 'Aucun paiement encaissé : la commande sera clôturée.'}
                            champMotif="Motif de l'annulation" motifObligatoire libelleConfirmer="Annuler la commande" destructif
                            onConfirmer={motif => agir('annuler', 'Commande annulée.', { motif, regularisation: modeAnnulation })}
                        >
                            {payee && <ChoixRegularisation valeur={modeAnnulation} surChangement={setModeAnnulation} />}
                        </DialogueAction>
                    )}
                    {aRole(Roles.Commercial) && s === 'Livree' && (
                        <DialogueAction declencheur={<Button variant="outline"><Lock /> Clôturer</Button>} titre="Clôturer la commande"
                            description="Un ticket SAV restera possible après la clôture." libelleConfirmer="Clôturer"
                            onConfirmer={() => agir('cloturer', 'Commande clôturée.', {})} />
                    )}
                    {aRole() && !['Livree', 'Cloturee'].includes(s) && (
                        <DialogueAction declencheur={<Button variant="ghost"><Lock /> Clôture exceptionnelle</Button>} titre="Clôture exceptionnelle"
                            description="Réservée à l'Administrateur. Impossible si de l'argent ou du stock reste à régulariser."
                            champMotif="Motif" motifObligatoire libelleConfirmer="Clôturer" destructif
                            onConfirmer={motif => agir('cloturer', 'Commande clôturée.', { motif })} />
                    )}
                </div>
            </div>

            <div className="mb-6 grid gap-3 sm:grid-cols-3">
                <Card><CardHeader className="pb-2"><CardDescription>Total (version {c.versionActive})</CardDescription><CardTitle className="text-xl tabular-nums">{formatFcfa(c.total)}</CardTitle></CardHeader></Card>
                <Card className={cn(c.resteAPayer > 0 && 'border-amber-300')}>
                    <CardHeader className="pb-2">
                        <CardDescription>{c.resteAPayer < 0 ? 'Trop-perçu à rendre' : 'Reste à payer'}</CardDescription>
                        <CardTitle className="text-xl tabular-nums">{formatFcfa(Math.abs(c.resteAPayer))}</CardTitle>
                    </CardHeader>
                    {c.dateLimitePaiement && <CardContent className="text-muted-foreground text-xs">Avant le {formatDate(c.dateLimitePaiement)}</CardContent>}
                </Card>
                <Card>
                    <CardHeader className="pb-2"><CardDescription>Livraison</CardDescription></CardHeader>
                    <CardContent className="text-sm">
                        {c.adresseLivraison.ligne1}{c.adresseLivraison.ligne2 && `, ${c.adresseLivraison.ligne2}`}, {c.adresseLivraison.ville}
                        <div className="text-muted-foreground">{c.adresseLivraison.telephoneContact}</div>
                    </CardContent>
                </Card>
            </div>

            {proposition && <PropositionEnCours commande={c} version={proposition} surAction={rafraichir} />}

            <Tabs defaultValue="contenu">
                <TabsList>
                    <TabsTrigger value="contenu">Contenu</TabsTrigger>
                    <TabsTrigger value="argent">Paiements ({c.paiements.length})</TabsTrigger>
                    {livraisons && livraisons.items.length > 0 && <TabsTrigger value="livraisons">Livraisons ({livraisons.items.length})</TabsTrigger>}
                    <TabsTrigger value="versions">Versions ({c.versions.length})</TabsTrigger>
                    {aRole(Roles.Commercial) && <TabsTrigger value="historique">Historique</TabsTrigger>}
                </TabsList>

                <TabsContent value="contenu">
                    <Card><CardContent className="p-0"><TableLignes lignes={c.lignes} /></CardContent></Card>
                </TabsContent>

                <TabsContent value="argent" className="space-y-4">
                    <Card>
                        <CardHeader><CardTitle className="text-base">Paiements</CardTitle></CardHeader>
                        <CardContent className="p-0">
                            {c.paiements.length === 0 ? <EtatVide message="Aucun paiement." /> : (
                                <Table>
                                    <TableHeader><TableRow><TableHead>Paiement</TableHead><TableHead>Mode</TableHead><TableHead>Version</TableHead><TableHead className="text-right">Montant</TableHead><TableHead>Statut</TableHead></TableRow></TableHeader>
                                    <TableBody>
                                        {c.paiements.map(p => (
                                            <TableRow key={p.id}>
                                                <TableCell><div className="font-medium">{p.reference}</div><div className="text-muted-foreground font-mono text-xs">{p.referenceExterne}</div></TableCell>
                                                <TableCell>{libelle(p.mode)}</TableCell>
                                                <TableCell>v{p.numeroVersion}</TableCell>
                                                <TableCell className="text-right tabular-nums">{formatFcfa(p.montant)}</TableCell>
                                                <TableCell><StatutBadge statut={p.statut} />{p.motifRejet && <div className="mt-1 text-xs text-red-600">{p.motifRejet}</div>}</TableCell>
                                            </TableRow>
                                        ))}
                                    </TableBody>
                                </Table>
                            )}
                        </CardContent>
                    </Card>
                    {(c.remboursements.length > 0 || c.avoirs.length > 0) && (
                        <Card>
                            <CardHeader><CardTitle className="text-base">Remboursements et avoirs</CardTitle></CardHeader>
                            <CardContent className="p-0">
                                <Table>
                                    <TableHeader><TableRow><TableHead>Référence</TableHead><TableHead>Motif</TableHead><TableHead className="text-right">Montant</TableHead><TableHead>Statut</TableHead></TableRow></TableHeader>
                                    <TableBody>
                                        {[...c.remboursements.map(r => ({ ...r, type: 'Remboursement' })), ...c.avoirs.map(a => ({ ...a, type: 'Avoir' }))].map(r => (
                                            <TableRow key={r.id}>
                                                <TableCell><div className="font-medium">{r.reference}</div><div className="text-muted-foreground text-xs">{r.type} · v{r.numeroVersion}</div></TableCell>
                                                <TableCell className="max-w-xs truncate">{r.motif}</TableCell>
                                                <TableCell className="text-right tabular-nums">{formatFcfa(r.montant)}</TableCell>
                                                <TableCell><StatutBadge statut={r.statut} /></TableCell>
                                            </TableRow>
                                        ))}
                                    </TableBody>
                                </Table>
                            </CardContent>
                        </Card>
                    )}
                </TabsContent>

                {livraisons && (
                    <TabsContent value="livraisons">
                        <Card><CardContent className="p-0">
                            <Table>
                                <TableHeader><TableRow><TableHead>Livraison</TableHead><TableHead>Livreur</TableHead><TableHead>Prévue le</TableHead><TableHead>Statut</TableHead></TableRow></TableHeader>
                                <TableBody>
                                    {livraisons.items.map(l => (
                                        <TableRow key={l.id}>
                                            <TableCell><div className="font-medium">{l.reference}</div><div className="text-muted-foreground text-xs">{libelle(l.type)}</div></TableCell>
                                            <TableCell>{l.livreurNom ?? '—'}</TableCell>
                                            <TableCell>{formatDate(l.datePlanifiee, false)}</TableCell>
                                            <TableCell><StatutBadge statut={l.statut} />{l.motifEchec && <div className="mt-1 text-xs text-red-600">{l.motifEchec}</div>}</TableCell>
                                        </TableRow>
                                    ))}
                                </TableBody>
                            </Table>
                        </CardContent></Card>
                    </TabsContent>
                )}

                <TabsContent value="versions" className="space-y-4">
                    {[...c.versions].reverse().map(v => (
                        <Card key={v.id} className={cn(v.active && 'border-primary')}>
                            <CardHeader>
                                <CardTitle className="flex flex-wrap items-center gap-2 text-base">
                                    Version {v.numeroVersion} <StatutBadge statut={v.statut} />
                                    {v.active && <span className="text-primary text-xs font-normal">active</span>}
                                    <span className="ml-auto tabular-nums">{formatFcfa(v.total)}</span>
                                </CardTitle>
                                <CardDescription>
                                    {formatDate(v.dateCreation)}{v.motifModification && ` · ${v.motifModification}`}{v.motifRefus && ` · ${v.motifRefus}`}
                                </CardDescription>
                            </CardHeader>
                            <CardContent className="p-0"><TableLignes lignes={v.lignes} /></CardContent>
                        </Card>
                    ))}
                </TabsContent>

                {aRole(Roles.Commercial) && (
                    <TabsContent value="historique">
                        <Card><CardContent className="pt-6"><Historique commandeId={c.id} /></CardContent></Card>
                    </TabsContent>
                )}
            </Tabs>
        </>
    );
}
