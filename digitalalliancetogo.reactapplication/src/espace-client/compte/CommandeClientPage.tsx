import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, Check, Clock, Loader2, Smartphone, Truck, Wallet, XCircle } from 'lucide-react';
import { toast } from 'sonner';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Chargement, EtatErreur, StatutBadge } from '@/components/commun';
import { DialogueAction } from '@/components/DialogueAction';
import { EnvoiFichier } from '@/components/Fichiers';
import { api } from '@/lib/api';
import { formatDate, formatFcfa, libelle } from '@/lib/format';
import type { Avoir, CommandeDetail, PaginatedList, VersionCommande } from '@/lib/types';
import { cn } from '@/lib/utils';
import { DialogueNouveauTicket } from '@/pages/sav/DialogueNouveauTicket';
import { STATUT_CLIENT } from '../client';

type Mode = 'Remboursement' | 'Avoir';

/** Étapes lisibles pour le client, déduites du statut interne. */
const ETAPES = ['Commandée', 'Payée', 'Préparée', 'En livraison', 'Livrée'] as const;
function etapeAtteinte(statut: string): number {
    switch (statut) {
        case 'CommandeCreee': case 'PaiementEnAttente': case 'PaiementEchoue': return 0;
        case 'PaiementConfirme': case 'EnAttenteDisponibilite': case 'StockReserve': case 'PreparationEnCours': return 1;
        case 'PretePourLivraison': return 2;
        case 'EnTransit': return 3;
        case 'Livree': case 'Cloturee': return 4;
        default: return -1; // annulée, refusée...
    }
}

function Etapes({ statut }: { statut: string }) {
    const atteinte = etapeAtteinte(statut);
    if (atteinte < 0) return null;
    return (
        <ol className="mb-6 grid grid-cols-5 gap-1">
            {ETAPES.map((e, i) => (
                <li key={e} className="flex flex-col items-center gap-1 text-center">
                    <span className={cn('flex size-7 items-center justify-center rounded-full border text-xs',
                        i <= atteinte ? 'bg-primary text-primary-foreground border-primary' : 'text-muted-foreground')}>
                        {i < atteinte || (i === atteinte && i === 4) ? <Check className="size-4" /> : i + 1}
                    </span>
                    <span className={cn('text-[11px] leading-tight', i <= atteinte ? 'font-medium' : 'text-muted-foreground')}>{e}</span>
                </li>
            ))}
        </ol>
    );
}

function Lignes({ version }: { version: Pick<VersionCommande, 'lignes' | 'total' | 'remise'> }) {
    return (
        <div className="divide-y">
            {version.lignes.map(l => (
                <div key={l.id} className="flex items-center justify-between gap-3 py-2 text-sm">
                    <span>{l.quantite} × {l.produitNom}</span>
                    <span className="tabular-nums">{formatFcfa(l.total)}</span>
                </div>
            ))}
            {version.remise > 0 && <div className="text-muted-foreground flex justify-between py-2 text-sm"><span>Remise</span><span>− {formatFcfa(version.remise)}</span></div>}
            <div className="flex justify-between py-2 font-semibold"><span>Total</span><span className="tabular-nums">{formatFcfa(version.total)}</span></div>
        </div>
    );
}

/** Paiement par Mobile Money (référence + capture) ou avec un avoir disponible. */
function Paiement({ commande, surPaye }: { commande: CommandeDetail; surPaye: () => void }) {
    const [reference, setReference] = useState('');
    const [preuveUrl, setPreuveUrl] = useState<string | null>(null);
    const { data: avoirs } = useQuery({
        queryKey: ['mes-avoirs', 'disponibles'],
        queryFn: () => api.get<PaginatedList<Avoir>>('/avoirs', { statut: 'Disponible', pageSize: 50 }),
    });

    const payer = useMutation({
        mutationFn: () => api.post(`/commandes/${commande.id}/paiements`, { referenceExterne: reference.trim(), preuveUrl }),
        onSuccess: () => { toast.success('Merci ! Votre paiement est en cours de vérification.'); surPaye(); },
        onError: e => toast.error(e.message),
    });
    const payerAvecAvoir = useMutation({
        mutationFn: (avoirId: string) => api.post<{ resteAPayer: number }>(`/commandes/${commande.id}/payer-avec-avoir`, { avoirId }),
        onSuccess: r => { toast.success(r.resteAPayer > 0 ? `Avoir utilisé. Il reste ${formatFcfa(r.resteAPayer)} à payer.` : 'Commande payée avec votre avoir.'); surPaye(); },
        onError: e => toast.error(e.message),
    });
    const rejete = [...commande.paiements].reverse().find(p => p.statut === 'Echoue');

    return (
        <Card className="mb-6 border-primary">
            <CardHeader><CardTitle className="text-base">Payer {formatFcfa(commande.resteAPayer)}</CardTitle></CardHeader>
            <CardContent className="grid gap-4">
                {commande.statut === 'PaiementEchoue' && rejete?.motifRejet && (
                    <Alert variant="destructive"><XCircle /><AlertTitle>Paiement précédent refusé</AlertTitle><AlertDescription>{rejete.motifRejet}</AlertDescription></Alert>
                )}
                <div className="grid gap-2 text-sm">
                    <div className="flex items-center gap-2 font-medium"><Smartphone className="size-4" /> Mobile Money (T-Money, Flooz)</div>
                    <p className="text-muted-foreground">Envoyez {formatFcfa(commande.resteAPayer)} au numéro de paiement de Togo Informatique, puis indiquez ci-dessous la référence reçue par SMS.</p>
                </div>
                <div className="grid gap-2">
                    <Label htmlFor="ref">Référence de la transaction *</Label>
                    <Input id="ref" placeholder="Ex : identifiant du SMS de confirmation" value={reference} onChange={e => setReference(e.target.value)} maxLength={100} />
                </div>
                <div className="grid gap-2">
                    <Label>Capture d'écran du paiement (conseillé)</Label>
                    <EnvoiFichier categorie="paiements" valeur={preuveUrl} surChangement={setPreuveUrl} libelle="Joindre la capture" />
                </div>
                <Button disabled={!reference.trim() || payer.isPending} onClick={() => payer.mutate()}>
                    {payer.isPending && <Loader2 className="animate-spin" />} J'ai payé : envoyer la référence
                </Button>
                {commande.dateLimitePaiement && <p className="text-muted-foreground text-xs">À régler avant le {formatDate(commande.dateLimitePaiement)}, sinon la commande sera annulée.</p>}

                {!!avoirs?.items.length && (
                    <div className="grid gap-2 border-t pt-4">
                        <div className="flex items-center gap-2 text-sm font-medium"><Wallet className="size-4" /> Utiliser un avoir</div>
                        {avoirs.items.map(a => (
                            <Button key={a.id} variant="outline" className="justify-between" disabled={payerAvecAvoir.isPending} onClick={() => payerAvecAvoir.mutate(a.id)}>
                                <span>{a.reference}</span><span className="tabular-nums">{formatFcfa(a.montantRestant)}</span>
                            </Button>
                        ))}
                    </div>
                )}
            </CardContent>
        </Card>
    );
}

export function CommandeClientPage() {
    const { id = '' } = useParams();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const [mode, setMode] = useState<Mode>('Remboursement');
    const { data: c, isPending, error } = useQuery({
        queryKey: ['ma-commande', id],
        queryFn: () => api.get<CommandeDetail>(`/commandes/${id}`),
    });
    const rafraichir = () => { for (const cle of ['ma-commande', 'mes-commandes', 'mes-avoirs']) void queryClient.invalidateQueries({ queryKey: [cle] }); };
    const agir = (chemin: string, corps: unknown, succes: string) =>
        api.post<{ message?: string }>(`/commandes/${id}/${chemin}`, corps)
            .then(r => { toast.success(r?.message ?? succes); rafraichir(); })
            .catch((e: Error) => { toast.error(e.message); throw e; });

    if (isPending) return <Chargement lignes={6} />;
    if (error) return <EtatErreur erreur={error} />;

    const s = c.statut;
    const aPayer = (s === 'CommandeCreee' || s === 'PaiementEchoue') && c.resteAPayer > 0;
    const proposition = c.versions.find(v => v.statut === 'EnAttenteClient');
    const actuelle = c.versions.find(v => v.active);
    const annulable = ['CommandeCreee', 'PaiementEchoue', 'PaiementConfirme', 'EnAttenteDisponibilite', 'StockReserve', 'PreparationEnCours', 'PretePourLivraison'].includes(s);
    const dejaPaye = c.paiements.some(p => p.statut === 'Confirme');

    return (
        <>
            <Button asChild variant="ghost" size="sm" className="mb-2 -ml-2"><Link to="/compte"><ArrowLeft /> Mes commandes</Link></Button>
            <div className="mb-4 flex flex-wrap items-center justify-between gap-2">
                <div>
                    <h1 className="text-xl font-semibold">{c.reference}</h1>
                    <p className="text-muted-foreground text-sm">Passée le {formatDate(c.dateCreation, false)}</p>
                </div>
                <span className="font-medium">{STATUT_CLIENT[s] ?? libelle(s)}</span>
            </div>

            <Etapes statut={s} />

            {s === 'PaiementEnAttente' && (
                <Alert className="mb-6"><Clock /><AlertTitle>Paiement en cours de vérification</AlertTitle>
                    <AlertDescription>Nous vérifions votre transaction ; vous serez prévenu dès sa confirmation.</AlertDescription></Alert>
            )}
            {s === 'EnAttenteDisponibilite' && (
                <Alert className="mb-6"><Clock /><AlertTitle>Paiement reçu, merci !</AlertTitle>
                    <AlertDescription>Un produit est en cours de réapprovisionnement : votre commande part dès son arrivée.</AlertDescription></Alert>
            )}
            {aPayer && <Paiement commande={c} surPaye={rafraichir} />}

            {proposition && actuelle && (
                <Card className="mb-6 border-amber-300">
                    <CardHeader><CardTitle className="text-base">Modification proposée par notre commercial</CardTitle></CardHeader>
                    <CardContent className="grid gap-3">
                        {proposition.motifModification && <p className="text-sm">{proposition.motifModification}</p>}
                        <Lignes version={proposition} />
                        <p className="text-sm">
                            {proposition.total > actuelle.total
                                ? `Si vous acceptez, il restera ${formatFcfa(proposition.total - actuelle.total)} à payer.`
                                : proposition.total < actuelle.total ? `Si vous acceptez, ${formatFcfa(actuelle.total - proposition.total)} vous seront rendus.` : 'Même montant.'}
                        </p>
                        {proposition.total < actuelle.total && (
                            <Select value={mode} onValueChange={v => setMode(v as Mode)}>
                                <SelectTrigger><SelectValue /></SelectTrigger>
                                <SelectContent>
                                    <SelectItem value="Remboursement">Être remboursé</SelectItem>
                                    <SelectItem value="Avoir">Recevoir un avoir</SelectItem>
                                </SelectContent>
                            </Select>
                        )}
                        <div className="flex gap-2">
                            <Button variant="outline" onClick={() => void agir('modifications/reponse', { accepter: false }, 'Modification refusée.')}>Refuser</Button>
                            <Button onClick={() => void agir('modifications/reponse', { accepter: true, regularisation: mode }, 'Modification acceptée.')}>Accepter</Button>
                        </div>
                    </CardContent>
                </Card>
            )}

            <div className="grid gap-6 md:grid-cols-2">
                <Card>
                    <CardHeader><CardTitle className="text-base">Contenu</CardTitle></CardHeader>
                    <CardContent>{actuelle && <Lignes version={actuelle} />}</CardContent>
                </Card>
                <Card>
                    <CardHeader><CardTitle className="text-base">Livraison</CardTitle></CardHeader>
                    <CardContent className="grid gap-3 text-sm">
                        <p>{c.adresseLivraison.ligne1}{c.adresseLivraison.ligne2 && `, ${c.adresseLivraison.ligne2}`}, {c.adresseLivraison.ville}<br />
                            <span className="text-muted-foreground">Contact : {c.adresseLivraison.telephoneContact}</span></p>
                        {c.livraisons.map(l => (
                            <div key={l.reference} className="flex items-start gap-2 rounded-md border p-2">
                                <Truck className="text-muted-foreground mt-0.5 size-4" />
                                <div className="flex-1">
                                    <div>{libelle(l.type)} · {l.dateLivraison ? `livrée le ${formatDate(l.dateLivraison, false)}` : `prévue le ${formatDate(l.datePlanifiee, false)}`}</div>
                                    {l.motifEchec && <div className="text-muted-foreground text-xs">{l.motifEchec}</div>}
                                </div>
                                <StatutBadge statut={l.statut} />
                            </div>
                        ))}
                        {c.livraisons.length === 0 && <p className="text-muted-foreground">La date de livraison vous sera communiquée après la préparation.</p>}
                    </CardContent>
                </Card>
            </div>

            {(c.remboursements.length > 0 || c.avoirs.length > 0) && (
                <Card className="mt-6">
                    <CardHeader><CardTitle className="text-base">Argent rendu</CardTitle></CardHeader>
                    <CardContent className="grid gap-2 text-sm">
                        {[...c.remboursements.map(r => ({ ...r, type: 'Remboursement' })), ...c.avoirs.map(a => ({ ...a, type: 'Avoir' }))].map(r => (
                            <div key={r.id} className="flex items-center justify-between gap-2">
                                <span>{r.type} de {formatFcfa(r.montant)}</span><StatutBadge statut={r.statut} />
                            </div>
                        ))}
                    </CardContent>
                </Card>
            )}

            <div className="mt-6 flex flex-wrap gap-2">
                {(s === 'Livree' || s === 'Cloturee') && (
                    <DialogueNouveauTicket commandeId={c.id} surCree={() => navigate('/compte/sav')} />
                )}
                {annulable && (
                    <DialogueAction declencheur={<Button variant="ghost" className="text-destructive">Annuler la commande</Button>}
                        titre="Annuler la commande ?" champMotif="Pourquoi ? (facultatif pour vous, utile pour nous)" libelleConfirmer="Annuler la commande" destructif
                        description={dejaPaye ? 'Votre paiement vous sera rendu, selon votre choix.' : undefined}
                        onConfirmer={motif => agir('annuler', { motif: motif || 'Annulée par le client', regularisation: mode }, 'Commande annulée.')}>
                        {dejaPaye && (
                            <Select value={mode} onValueChange={v => setMode(v as Mode)}>
                                <SelectTrigger><SelectValue /></SelectTrigger>
                                <SelectContent>
                                    <SelectItem value="Remboursement">Être remboursé</SelectItem>
                                    <SelectItem value="Avoir">Recevoir un avoir</SelectItem>
                                </SelectContent>
                            </Select>
                        )}
                    </DialogueAction>
                )}
            </div>
        </>
    );
}
