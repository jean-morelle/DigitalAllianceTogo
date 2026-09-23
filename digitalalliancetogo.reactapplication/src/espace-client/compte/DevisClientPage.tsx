import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, Check, Clock } from 'lucide-react';
import { toast } from 'sonner';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { Chargement, EtatErreur, StatutBadge } from '@/components/commun';
import { DialogueAction } from '@/components/DialogueAction';
import { api } from '@/lib/api';
import { formatDate, formatFcfa } from '@/lib/format';
import type { DevisDetail } from '@/lib/types';
import { ChoixAdresse } from '../Adresses';
import { adresseParDefaut, useMaFiche } from '../client';

export function DevisClientPage() {
    const { id = '' } = useParams();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const { data: fiche } = useMaFiche();
    const [adresseChoisie, setAdresseChoisie] = useState('');
    const { data: d, isPending, error } = useQuery({
        queryKey: ['mon-devis', id],
        queryFn: () => api.get<DevisDetail>(`/devis/${id}`),
    });

    const repondre = (reponse: 'Refuser' | 'DemanderModification', commentaire: string) =>
        api.post(`/devis/${id}/repondre`, { reponse, commentaire: commentaire || null })
            .then(() => {
                toast.success(reponse === 'Refuser' ? 'Devis refusé.' : 'Demande envoyée à votre commercial.');
                void queryClient.invalidateQueries({ queryKey: ['mon-devis'] });
                void queryClient.invalidateQueries({ queryKey: ['mes-devis'] });
            })
            .catch((e: Error) => { toast.error(e.message); throw e; });

    if (isPending) return <Chargement />;
    if (error) return <EtatErreur erreur={error} />;

    const adresseId = adresseParDefaut(fiche?.adresses, adresseChoisie);
    const enPreparation = d.statut === 'Brouillon' || d.statut === 'ValidationInterne';

    return (
        <>
            <Button asChild variant="ghost" size="sm" className="mb-2 -ml-2"><Link to="/compte/devis"><ArrowLeft /> Mes devis</Link></Button>
            <div className="mb-4 flex flex-wrap items-center justify-between gap-2">
                <div>
                    <h1 className="text-xl font-semibold">Devis {d.reference}</h1>
                    <p className="text-muted-foreground text-sm">
                        {d.statut === 'Envoye' ? `Valable jusqu'au ${formatDate(d.dateValidite, false)}` : `Demandé le ${formatDate(d.dateCreation, false)}`}
                    </p>
                </div>
                <StatutBadge statut={enPreparation ? 'EnAttente' : d.statut} />
            </div>

            {enPreparation && (
                <Alert className="mb-4"><Clock /><AlertTitle>Votre commercial prépare ce devis</AlertTitle>
                    <AlertDescription>Vous serez prévenu dès qu'il vous sera envoyé.</AlertDescription></Alert>
            )}

            <Card className="mb-6">
                <CardHeader><CardTitle className="text-base">Détail</CardTitle></CardHeader>
                <CardContent className="divide-y">
                    {d.lignes.map(l => (
                        <div key={l.id} className="flex items-center justify-between gap-3 py-2 text-sm">
                            <span>{l.quantite} × {l.produitNom}{l.remise > 0 && <span className="text-emerald-700"> (remise {formatFcfa(l.remise)})</span>}</span>
                            <span className="tabular-nums">{formatFcfa(l.total)}</span>
                        </div>
                    ))}
                    {d.remise > 0 && <div className="flex justify-between py-2 text-sm text-emerald-700"><span>Remise totale</span><span>− {formatFcfa(d.remise)}</span></div>}
                    <div className="flex justify-between py-2 font-semibold"><span>Total</span><span className="tabular-nums">{formatFcfa(d.total)}</span></div>
                </CardContent>
            </Card>

            {d.statut === 'Envoye' && (
                <Card className="border-primary">
                    <CardHeader><CardTitle className="text-base">Votre réponse</CardTitle></CardHeader>
                    <CardContent className="grid gap-4">
                        <div className="grid gap-2">
                            <Label>Adresse de livraison si vous acceptez</Label>
                            <ChoixAdresse valeur={adresseId} surChangement={setAdresseChoisie} />
                        </div>
                        <div className="flex flex-wrap gap-2">
                            <DialogueAction declencheur={<Button><Check /> Accepter le devis</Button>} titre="Accepter le devis"
                                description={`La commande de ${formatFcfa(d.total)} sera créée ; vous la paierez ensuite par Mobile Money.`}
                                libelleConfirmer="Accepter"
                                onConfirmer={() => api.post<string>(`/devis/${id}/accepter`, { adresseLivraisonId: adresseId, telephoneContact: null })
                                    .then(commandeId => {
                                        toast.success('Commande créée.');
                                        void queryClient.invalidateQueries({ queryKey: ['mes-commandes'] });
                                        navigate(`/compte/commandes/${commandeId}`);
                                    })
                                    .catch((e: Error) => { toast.error(e.message); throw e; })} />
                            <DialogueAction declencheur={<Button variant="outline">Demander une modification</Button>} titre="Que souhaitez-vous changer ?"
                                champMotif="Votre demande" motifObligatoire libelleConfirmer="Envoyer" onConfirmer={c => repondre('DemanderModification', c)} />
                            <DialogueAction declencheur={<Button variant="ghost">Refuser</Button>} titre="Refuser le devis"
                                champMotif="Raison (facultatif)" libelleConfirmer="Refuser" destructif onConfirmer={c => repondre('Refuser', c)} />
                        </div>
                    </CardContent>
                </Card>
            )}
            {d.commandeId && <Button asChild><Link to={`/compte/commandes/${d.commandeId}`}>Voir la commande</Link></Button>}
        </>
    );
}
