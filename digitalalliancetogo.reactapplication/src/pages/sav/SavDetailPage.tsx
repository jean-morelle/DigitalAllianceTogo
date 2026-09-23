import { useState } from 'react';
import { Link, useParams } from 'react-router';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, ClipboardCheck, PackageSearch, Scale, Stethoscope, Wrench } from 'lucide-react';
import { toast } from 'sonner';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { Chargement, EtatErreur, StatutBadge } from '@/components/commun';
import { DialogueAction } from '@/components/DialogueAction';
import { api } from '@/lib/api';
import { useAuth } from '@/lib/auth';
import { formatDate, formatFcfa, libelle } from '@/lib/format';
import { Roles } from '@/lib/roles';
import type { Entrepot, TicketSavDetail } from '@/lib/types';
import { BoutonRemettre, DialoguePlanifier } from '@/pages/livraisons/ActionsLivraison';

type Decision = 'Remplacement' | 'Remboursement' | 'Avoir';

function Oui_Non({ valeur, surChangement, oui, non }: { valeur: boolean; surChangement: (v: boolean) => void; oui: string; non: string }) {
    return (
        <Select value={valeur ? 'oui' : 'non'} onValueChange={v => surChangement(v === 'oui')}>
            <SelectTrigger><SelectValue /></SelectTrigger>
            <SelectContent>
                <SelectItem value="oui">{oui}</SelectItem>
                <SelectItem value="non">{non}</SelectItem>
            </SelectContent>
        </Select>
    );
}

export function SavDetailPage() {
    const { id = '' } = useParams();
    const { aRole } = useAuth();
    const queryClient = useQueryClient();
    const [reparable, setReparable] = useState(true);
    const [recommandation, setRecommandation] = useState('');
    const [description, setDescription] = useState('');
    const [testReussi, setTestReussi] = useState(true);
    const [decision, setDecision] = useState<Decision>('Remplacement');
    const [entrepotId, setEntrepotId] = useState('');
    const [defectueux, setDefectueux] = useState(true);

    const { data: t, isPending, error } = useQuery({
        queryKey: ['ticket-sav', id],
        queryFn: () => api.get<TicketSavDetail>(`/sav/${id}`),
    });
    const { data: entrepots } = useQuery({
        queryKey: ['entrepots'],
        queryFn: () => api.get<Entrepot[]>('/stocks/entrepots'),
        enabled: aRole(Roles.GestionnaireStock),
    });

    const agir = (chemin: string, corps: unknown, succes?: string) =>
        api.post<{ message?: string } | undefined>(`/sav/${id}/${chemin}`, corps)
            .then(r => {
                toast.success(r?.message ?? succes ?? 'Enregistré.');
                for (const cle of ['ticket-sav', 'sav', 'a-traiter', 'stocks', 'livraisons']) void queryClient.invalidateQueries({ queryKey: [cle] });
            })
            .catch((e: Error) => { toast.error(e.message); throw e; });

    if (isPending) return <Chargement lignes={6} />;
    if (error) return <EtatErreur erreur={error} />;

    const s = t.statut;
    const livraisonEnCours = t.livraisons.some(l => l.statut === 'Planifiee' || l.statut === 'EnTransit');
    const entrepot = entrepotId || (entrepots?.length === 1 ? entrepots[0].id : '');
    // Remplacement déjà en cours : on ne peut que basculer vers remboursement ou avoir
    const decisionEffective: Decision = s === 'RemplacementEnCours' && decision === 'Remplacement' ? 'Remboursement' : decision;

    return (
        <>
            <Button asChild variant="ghost" size="sm" className="mb-2 -ml-2">
                <Link to="/sav"><ArrowLeft /> SAV</Link>
            </Button>

            <div className="mb-6 flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                <div>
                    <h1 className="flex flex-wrap items-center gap-3 text-2xl font-semibold">{t.reference} <StatutBadge statut={s} /></h1>
                    <p className="text-muted-foreground mt-1 text-sm">
                        {t.produitNom}{t.quantite > 1 && ` × ${t.quantite}`} · commande{' '}
                        {aRole(Roles.Commercial, Roles.GestionnaireStock)
                            ? <Link className="text-primary hover:underline" to={`/commandes/${t.commandeId}`}>{t.commandeReference}</Link>
                            : t.commandeReference}
                        {' '}· client <span className="font-mono">{t.codeClient}</span> · ouvert le {formatDate(t.dateCreation)}
                    </p>
                </div>

                <div className="flex flex-wrap gap-2">
                    {/* Technicien : diagnostic puis réparation */}
                    {s === 'Ouvert' && aRole(Roles.Technicien) && (
                        <DialogueAction declencheur={<Button><Stethoscope /> Diagnostiquer</Button>} titre="Diagnostic"
                            champMotif="Conclusion du diagnostic" motifObligatoire libelleConfirmer="Enregistrer"
                            onConfirmer={conclusion => agir('diagnostic', { conclusion, reparable, recommandation: recommandation.trim() || null },
                                reparable ? 'Réparation à effectuer.' : 'Produit irréparable : décision commerciale attendue.')}>
                            <div className="grid gap-3">
                                <Oui_Non valeur={reparable} surChangement={setReparable} oui="Réparable" non="Irréparable" />
                                {!reparable && (
                                    <div className="grid gap-2">
                                        <Label htmlFor="reco">Recommandation *</Label>
                                        <Input id="reco" placeholder="Ex : remplacement" value={recommandation} onChange={e => setRecommandation(e.target.value)} />
                                    </div>
                                )}
                            </div>
                        </DialogueAction>
                    )}
                    {s === 'EnReparation' && aRole(Roles.Technicien) && (
                        <DialogueAction declencheur={<Button><Wrench /> Terminer la réparation</Button>} titre="Fin de réparation"
                            champMotif="Résultat du test" libelleConfirmer="Enregistrer"
                            onConfirmer={resultat => agir('reparation', { description: description.trim(), resultat: resultat || null, testReussi },
                                testReussi ? 'Réparé : ticket clôturé, le Commercial prévient le client.' : 'Réparation impossible : décision commerciale attendue.')}>
                            <div className="grid gap-3">
                                <div className="grid gap-2">
                                    <Label htmlFor="intervention">Intervention réalisée *</Label>
                                    <Textarea id="intervention" value={description} onChange={e => setDescription(e.target.value)} maxLength={2000} />
                                </div>
                                <Oui_Non valeur={testReussi} surChangement={setTestReussi} oui="Test réussi : produit réparé" non="Test raté : produit irréparable" />
                            </div>
                        </DialogueAction>
                    )}

                    {/* Commercial : choix du client pour un produit irréparable */}
                    {(s === 'DecisionCommerciale' || (s === 'RemplacementEnCours' && !livraisonEnCours)) && aRole(Roles.Commercial) && (
                        <DialogueAction declencheur={<Button variant={s === 'DecisionCommerciale' ? 'default' : 'outline'}><Scale /> {s === 'DecisionCommerciale' ? 'Enregistrer le choix du client' : 'Changer la décision'}</Button>}
                            titre="Choix du client" description="Remboursement et avoir seront validés par l'Administrateur."
                            libelleConfirmer="Enregistrer" onConfirmer={() => agir('decision', { decision: decisionEffective })}>
                            <Select value={decisionEffective} onValueChange={v => setDecision(v as Decision)}>
                                <SelectTrigger><SelectValue /></SelectTrigger>
                                <SelectContent>
                                    {s === 'DecisionCommerciale' && <SelectItem value="Remplacement">Remplacement par le même produit</SelectItem>}
                                    <SelectItem value="Remboursement">Remboursement</SelectItem>
                                    <SelectItem value="Avoir">Avoir</SelectItem>
                                </SelectContent>
                            </Select>
                        </DialogueAction>
                    )}

                    {/* Stock : livraison du remplacement, puis ancien produit */}
                    {s === 'RemplacementEnCours' && !livraisonEnCours && aRole(Roles.GestionnaireStock) && <DialoguePlanifier ticketId={t.id} />}
                    {t.decision && !t.ancienProduitReceptionne && aRole(Roles.GestionnaireStock) && (
                        <DialogueAction declencheur={<Button variant="outline"><PackageSearch /> Réceptionner l'ancien produit</Button>}
                            titre="Ancien produit récupéré chez le client" description="Contrôlez-le et classez-le."
                            libelleConfirmer="Enregistrer"
                            onConfirmer={() => {
                                if (!entrepot) { toast.error('Choisissez un entrepôt.'); return Promise.reject(new Error('Entrepôt manquant')); }
                                return agir('ancien-produit', { entrepotId: entrepot, defectueux }, defectueux ? 'Ancien produit classé défectueux.' : 'Ancien produit remis en stock.');
                            }}>
                            <div className="grid gap-3">
                                <Select value={entrepot} onValueChange={setEntrepotId}>
                                    <SelectTrigger><SelectValue placeholder="Entrepôt" /></SelectTrigger>
                                    <SelectContent>{entrepots?.filter(e => e.actif).map(e => <SelectItem key={e.id} value={e.id}>{e.nom}</SelectItem>)}</SelectContent>
                                </Select>
                                <Oui_Non valeur={defectueux} surChangement={setDefectueux} oui="Endommagé : défectueux" non="Réutilisable : disponible à la vente" />
                            </div>
                        </DialogueAction>
                    )}
                </div>
            </div>

            <Alert className="mb-6">
                <ClipboardCheck />
                <AlertTitle>Problème signalé</AlertTitle>
                <AlertDescription>{t.motif}</AlertDescription>
            </Alert>

            {t.resolution && (
                <Alert className="mb-6 border-emerald-300">
                    <AlertTitle>Résolution : {t.resolution}</AlertTitle>
                    <AlertDescription>Clôturé le {formatDate(t.dateCloture)}{t.decision && ` · décision : ${libelle(t.decision)}`}</AlertDescription>
                </Alert>
            )}

            <div className="grid gap-4 lg:grid-cols-2">
                <Card>
                    <CardHeader><CardTitle className="text-base">Technique</CardTitle></CardHeader>
                    <CardContent>
                        {t.diagnostics.length === 0 && t.interventions.length === 0 ? (
                            <p className="text-muted-foreground text-sm">En attente du diagnostic du technicien.</p>
                        ) : (
                            <ol className="relative ml-2 border-l pl-6">
                                {[...t.diagnostics.map(d => ({ date: d.date, titre: d.reparable ? 'Diagnostic : réparable' : 'Diagnostic : irréparable', texte: d.conclusion, detail: d.recommandation })),
                                  ...t.interventions.map(i => ({ date: i.dateDebut, titre: 'Intervention', texte: i.description, detail: i.resultat }))]
                                    .sort((a, b) => a.date.localeCompare(b.date))
                                    .map((e, i) => (
                                        <li key={i} className="mb-4">
                                            <span className="bg-primary absolute -left-1.5 mt-1.5 size-3 rounded-full" />
                                            <div className="text-sm font-medium">{e.titre}</div>
                                            <div className="text-sm">{e.texte}</div>
                                            {e.detail && <div className="text-muted-foreground text-sm">{e.detail}</div>}
                                            <div className="text-muted-foreground text-xs">{formatDate(e.date)}</div>
                                        </li>
                                    ))}
                            </ol>
                        )}
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader><CardTitle className="text-base">Suite donnée</CardTitle></CardHeader>
                    <CardContent className="space-y-3 text-sm">
                        {t.decision ? <p>Décision : <strong>{libelle(t.decision)}</strong></p> : <p className="text-muted-foreground">Aucune décision commerciale (réparation en priorité).</p>}
                        {t.livraisons.map(l => (
                            <div key={l.id} className="flex items-center justify-between gap-2 rounded-md border p-2">
                                <div><div className="font-medium">{l.reference}</div><div className="text-muted-foreground text-xs">{libelle(l.type)}</div></div>
                                <div className="flex items-center gap-2">
                                    <StatutBadge statut={l.statut} />
                                    {l.statut === 'Planifiee' && aRole(Roles.GestionnaireStock) && <BoutonRemettre livraison={{ id: l.id, reference: l.reference, livreurNom: null }} />}
                                </div>
                            </div>
                        ))}
                        {t.regularisations.map(r => (
                            <div key={r.id} className="flex items-center justify-between gap-2 rounded-md border p-2">
                                <div><div className="font-medium">{r.reference}</div><div className="text-muted-foreground text-xs">{r.type} · {formatFcfa(r.montant)}</div></div>
                                <StatutBadge statut={r.statut} />
                            </div>
                        ))}
                        {t.decision && (
                            <p className="text-muted-foreground text-xs">
                                Ancien produit : {t.ancienProduitReceptionne ? 'réceptionné et contrôlé par le stock.' : 'pas encore récupéré.'}
                            </p>
                        )}
                        {s === 'RemplacementEnCours' && t.livraisons.some(l => l.statut === 'EnTransit') && (
                            <p className="text-muted-foreground text-xs">Le livreur confirme la remise depuis l'écran Livraisons.</p>
                        )}
                    </CardContent>
                </Card>
            </div>
        </>
    );
}
