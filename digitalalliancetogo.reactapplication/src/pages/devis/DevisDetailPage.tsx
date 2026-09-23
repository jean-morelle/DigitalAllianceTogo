import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, ArrowRight, Check, MessageSquareWarning, Pencil, Send, ShieldCheck, X } from 'lucide-react';
import { toast } from 'sonner';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Table, TableBody, TableCell, TableFooter, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Chargement, EtatErreur, StatutBadge } from '@/components/commun';
import { DialogueAction } from '@/components/DialogueAction';
import { nomClient } from '@/lib/lignes';
import { api } from '@/lib/api';
import { useAuth } from '@/lib/auth';
import { formatDate, formatFcfa } from '@/lib/format';
import type { ClientDetail, DevisDetail } from '@/lib/types';

interface ResultatValidation {
    valide: boolean;
    statut: string;
    message: string;
}

/** Le Commercial enregistre l'acceptation du client (téléphone, WhatsApp, boutique) : la commande est créée. */
function DialogueAcceptation({ devis, surAccepter }: { devis: DevisDetail; surAccepter: (adresseId: string, telephone: string) => Promise<unknown> }) {
    const [adresseId, setAdresseId] = useState('');
    const [telephone, setTelephone] = useState('');
    const { data: client } = useQuery({
        queryKey: ['client', devis.clientId],
        queryFn: () => api.get<ClientDetail>(`/clients/${devis.clientId}`),
    });
    const adresse = adresseId || client?.adresses[0]?.id || '';

    return (
        <DialogueAction
            declencheur={<Button><Check /> Acceptation du client</Button>}
            titre={`Le client accepte ${devis.reference}`}
            description="La commande est créée aussitôt avec les prix du devis ; le stock sera réservé après paiement."
            libelleConfirmer="Créer la commande"
            onConfirmer={() => surAccepter(adresse, telephone.trim())}
        >
            {client && client.adresses.length === 0 ? (
                <Alert variant="destructive">
                    <AlertDescription>Ce client n'a aucune adresse de livraison : ajoutez-en une à sa fiche d'abord.</AlertDescription>
                </Alert>
            ) : (
                <div className="grid gap-4">
                    <div className="grid gap-2">
                        <Label>Adresse de livraison</Label>
                        <Select value={adresse} onValueChange={setAdresseId}>
                            <SelectTrigger><SelectValue placeholder="Chargement..." /></SelectTrigger>
                            <SelectContent>
                                {client?.adresses.map(a => (
                                    <SelectItem key={a.id} value={a.id}>{a.libelle} — {a.ligne1}, {a.ville}</SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                    </div>
                    <div className="grid gap-2">
                        <Label htmlFor="tel">Téléphone du destinataire</Label>
                        <Input id="tel" placeholder={client?.telephone ?? ''} value={telephone} onChange={e => setTelephone(e.target.value)} />
                        <p className="text-muted-foreground text-xs">Vide : celui de la fiche client.</p>
                    </div>
                </div>
            )}
        </DialogueAction>
    );
}

export function DevisDetailPage() {
    const { id = '' } = useParams();
    const navigate = useNavigate();
    const { aRole } = useAuth();
    const queryClient = useQueryClient();

    const { data: d, isPending, error } = useQuery({
        queryKey: ['devis-detail', id],
        queryFn: () => api.get<DevisDetail>(`/devis/${id}`),
    });
    const { data: client } = useQuery({
        queryKey: ['client', d?.clientId],
        queryFn: () => api.get<ClientDetail>(`/clients/${d!.clientId}`),
        enabled: !!d,
    });

    const rafraichir = () => {
        for (const cle of ['devis', 'devis-detail', 'a-traiter']) void queryClient.invalidateQueries({ queryKey: [cle] });
    };
    const action = useMutation({
        mutationFn: ({ chemin, corps }: { chemin: string; corps?: unknown }) => api.post<unknown>(`/devis/${id}/${chemin}`, corps),
        onSuccess: rafraichir,
        onError: e => toast.error(e.message),
    });
    const agir = async (chemin: string, succes: string, corps?: unknown) => {
        const r = await action.mutateAsync({ chemin, corps });
        const v = r as ResultatValidation | undefined;
        if (v && typeof v === 'object' && 'valide' in v) (v.valide ? toast.success : toast.warning)(v.message);
        else toast.success(succes);
        return r;
    };

    if (isPending) return <Chargement lignes={6} />;
    if (error) return <EtatErreur erreur={error} />;

    const modifiable = ['Brouillon', 'ValidationInterne', 'ModificationDemandee'].includes(d.statut);
    const aValider = (d.statut === 'Brouillon' || d.statut === 'ValidationInterne') && !d.valideParEntreprise;
    const aEnvoyer = (d.statut === 'Brouillon' || d.statut === 'ValidationInterne') && d.valideParEntreprise;
    const remiseLignes = d.lignes.reduce((s, l) => s + l.remise, 0);

    return (
        <>
            <Button asChild variant="ghost" size="sm" className="mb-2 -ml-2">
                <Link to="/devis"><ArrowLeft /> Devis</Link>
            </Button>

            <div className="mb-6 flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                <div>
                    <h1 className="flex flex-wrap items-center gap-3 text-2xl font-semibold">
                        {d.reference} <StatutBadge statut={d.statut} />
                        {aEnvoyer && <span className="text-sm font-normal text-emerald-700">validé par l'entreprise</span>}
                    </h1>
                    <p className="text-muted-foreground mt-1 text-sm">
                        {client ? `${nomClient(client)} · ` : ''}<span className="font-mono">{d.codeClient}</span>
                        {client && ` · ${client.telephone}`} · créé le {formatDate(d.dateCreation)}
                        {d.statut === 'Envoye' && ` · valable jusqu'au ${formatDate(d.dateValidite, false)}`}
                    </p>
                </div>

                <div className="flex flex-wrap gap-2">
                    {modifiable && (
                        <Button variant="outline" asChild><Link to={`/devis/${d.id}/modifier`}><Pencil /> Modifier</Link></Button>
                    )}
                    {d.statut === 'ValidationInterne' && aRole() && (
                        <DialogueAction declencheur={<Button variant="outline"><X /> Refuser la remise</Button>}
                            titre="Refuser la remise exceptionnelle" description="Le devis repasse en brouillon pour que le commercial revoie la remise."
                            champMotif="Motif (visible par le commercial)" motifObligatoire libelleConfirmer="Refuser" destructif
                            onConfirmer={motif => agir('refuser-validation', 'Remise refusée : devis renvoyé au commercial.', { motif })} />
                    )}
                    {aValider && (
                        <Button onClick={() => agir('valider', 'Devis validé.')}>
                            <ShieldCheck /> {d.statut === 'ValidationInterne' ? 'Valider la remise' : 'Valider'}
                        </Button>
                    )}
                    {aEnvoyer && (
                        <DialogueAction declencheur={<Button><Send /> Envoyer au client</Button>} titre="Envoyer le devis"
                            description="Le client pourra l'accepter, le refuser ou demander une modification jusqu'à sa date de validité."
                            libelleConfirmer="Envoyer" onConfirmer={() => agir('envoyer', 'Devis envoyé au client.')} />
                    )}
                    {d.statut === 'Envoye' && (
                        <>
                            <DialogueAction declencheur={<Button variant="outline"><MessageSquareWarning /> Modification demandée</Button>}
                                titre="Le client demande une modification" champMotif="Ce que le client souhaite" motifObligatoire
                                libelleConfirmer="Enregistrer"
                                onConfirmer={commentaire => agir('repondre', 'Demande de modification enregistrée.', { reponse: 'DemanderModification', commentaire })} />
                            <DialogueAction declencheur={<Button variant="outline"><X /> Refus du client</Button>}
                                titre="Le client refuse le devis" champMotif="Raison (facultatif)" libelleConfirmer="Enregistrer le refus" destructif
                                onConfirmer={commentaire => agir('repondre', 'Refus enregistré.', { reponse: 'Refuser', commentaire })} />
                            <DialogueAcceptation devis={d} surAccepter={async (adresseLivraisonId, telephoneContact) => {
                                const commandeId = await agir('accepter', 'Commande créée.', { adresseLivraisonId, telephoneContact: telephoneContact || null });
                                void queryClient.invalidateQueries({ queryKey: ['commandes'] });
                                navigate(`/commandes/${commandeId as string}`);
                            }} />
                        </>
                    )}
                    {d.commandeId && (
                        <Button asChild><Link to={`/commandes/${d.commandeId}`}>Voir la commande <ArrowRight /></Link></Button>
                    )}
                </div>
            </div>

            {d.statut === 'ValidationInterne' && (
                <Alert className="mb-6 border-amber-300">
                    <ShieldCheck />
                    <AlertTitle>Remise de {d.tauxRemise} % en attente de l'Administrateur</AlertTitle>
                    <AlertDescription>Au-delà du seuil commercial, seul l'Administrateur peut valider ce devis.</AlertDescription>
                </Alert>
            )}
            {d.commentaireInterne && (
                <Alert variant="destructive" className="mb-6">
                    <AlertTitle>Remise refusée par l'Administrateur</AlertTitle>
                    <AlertDescription>{d.commentaireInterne}</AlertDescription>
                </Alert>
            )}
            {d.commentaireClient && (
                <Alert className="mb-6">
                    <MessageSquareWarning />
                    <AlertTitle>Réponse du client</AlertTitle>
                    <AlertDescription>{d.commentaireClient}</AlertDescription>
                </Alert>
            )}

            <Card>
                <CardHeader>
                    <CardTitle className="text-base">Détail</CardTitle>
                    <CardDescription>Prix figés au moment du devis.</CardDescription>
                </CardHeader>
                <CardContent className="p-0">
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
                            {d.lignes.map(l => (
                                <TableRow key={l.id}>
                                    <TableCell><div className="font-medium">{l.produitNom}</div><div className="text-muted-foreground text-xs">{l.produitReference}</div></TableCell>
                                    <TableCell className="text-right tabular-nums">{l.quantite}</TableCell>
                                    <TableCell className="text-right tabular-nums">{formatFcfa(l.prixUnitaire)}</TableCell>
                                    <TableCell className="text-right tabular-nums">{l.remise ? formatFcfa(l.remise) : '—'}</TableCell>
                                    <TableCell className="text-right font-medium tabular-nums">{formatFcfa(l.total)}</TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                        <TableFooter>
                            <TableRow><TableCell colSpan={4} className="text-right">Sous-total</TableCell><TableCell className="text-right tabular-nums">{formatFcfa(d.sousTotal)}</TableCell></TableRow>
                            {d.remise - remiseLignes > 0 && (
                                <TableRow><TableCell colSpan={4} className="text-right">Remise globale</TableCell><TableCell className="text-right tabular-nums">− {formatFcfa(d.remise - remiseLignes)}</TableCell></TableRow>
                            )}
                            <TableRow><TableCell colSpan={4} className="text-right">Remise totale ({d.tauxRemise} %)</TableCell><TableCell className="text-right tabular-nums">− {formatFcfa(d.remise)}</TableCell></TableRow>
                            <TableRow><TableCell colSpan={4} className="text-right font-semibold">Total</TableCell><TableCell className="text-right text-base font-semibold tabular-nums">{formatFcfa(d.total)}</TableCell></TableRow>
                        </TableFooter>
                    </Table>
                </CardContent>
            </Card>
        </>
    );
}
