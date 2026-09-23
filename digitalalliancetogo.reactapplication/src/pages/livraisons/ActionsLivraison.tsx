import { useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { CalendarPlus, CheckCircle2, LocateFixed, PackageCheck, XCircle } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { DialogueAction } from '@/components/DialogueAction';
import { api } from '@/lib/api';
import type { Livraison, Livreur } from '@/lib/types';

function useRafraichirLivraisons() {
    const queryClient = useQueryClient();
    return () => {
        for (const cle of ['livraisons', 'commande', 'commandes', 'stocks', 'a-traiter', 'livreurs']) void queryClient.invalidateQueries({ queryKey: [cle] });
    };
}

/** Exécute un appel API, affiche le résultat et relaie l'erreur pour garder le dialogue ouvert. */
function executer(promesse: Promise<unknown>, succes: string, rafraichir: () => void) {
    return promesse
        .then(() => { toast.success(succes); rafraichir(); })
        .catch((e: Error) => { toast.error(e.message); throw e; });
}

/** Planifier la livraison d'une commande prête : livreur et date. */
export function DialoguePlanifier({ commandeId, relivraison }: { commandeId: string; relivraison?: boolean }) {
    const rafraichir = useRafraichirLivraisons();
    // Calculées une fois (pas pendant chaque rendu) : aujourd'hui au plus tôt, demain par défaut
    const [{ aujourdhui, demain }] = useState(() => ({
        aujourdhui: new Date().toISOString().slice(0, 10),
        demain: new Date(Date.now() + 86_400_000).toISOString().slice(0, 10),
    }));
    const [livreurId, setLivreurId] = useState('');
    const [date, setDate] = useState(demain);
    const { data: livreurs } = useQuery({ queryKey: ['livreurs'], queryFn: () => api.get<Livreur[]>('/livraisons/livreurs') });

    return (
        <DialogueAction
            declencheur={<Button><CalendarPlus /> {relivraison ? 'Planifier la relivraison' : 'Planifier la livraison'}</Button>}
            titre={relivraison ? 'Planifier une nouvelle tentative' : 'Planifier la livraison'}
            description="Le stock ne sort qu'au moment de la remise du colis au livreur."
            libelleConfirmer="Planifier"
            onConfirmer={() => {
                if (!livreurId) {
                    toast.error('Choisissez un livreur.');
                    return Promise.reject(new Error('Livreur manquant'));
                }
                return executer(api.post('/livraisons', { commandeId, livreurId, datePlanifiee: `${date}T08:00:00Z` }), 'Livraison planifiée.', rafraichir);
            }}
        >
            <div className="grid gap-4">
                <div className="grid gap-2">
                    <Label>Livreur</Label>
                    <Select value={livreurId} onValueChange={setLivreurId}>
                        <SelectTrigger><SelectValue placeholder={livreurs?.length === 0 ? 'Aucun livreur actif' : 'Choisir un livreur'} /></SelectTrigger>
                        <SelectContent>
                            {livreurs?.map(l => (
                                <SelectItem key={l.id} value={l.id}>
                                    {l.nom} · {l.livraisonsEnCours} en cours
                                </SelectItem>
                            ))}
                        </SelectContent>
                    </Select>
                </div>
                <div className="grid gap-2">
                    <Label htmlFor="date">Date prévue</Label>
                    <Input id="date" type="date" min={aujourdhui} value={date} onChange={e => setDate(e.target.value)} />
                </div>
            </div>
        </DialogueAction>
    );
}

export function BoutonRemettre({ livraison }: { livraison: Livraison }) {
    const rafraichir = useRafraichirLivraisons();
    return (
        <DialogueAction
            declencheur={<Button size="sm"><PackageCheck /> Remettre au livreur</Button>}
            titre={`Remettre ${livraison.reference} à ${livraison.livreurNom ?? 'le livreur'}`}
            description="C'est la sortie de stock : les produits réservés passent « en transit ». À faire au moment où le livreur emporte le colis."
            libelleConfirmer="Colis remis"
            onConfirmer={() => executer(api.post(`/livraisons/${livraison.id}/remettre`), 'Colis remis : la commande est en transit.', rafraichir)}
        />
    );
}

/** Le livreur confirme la remise au client, avec preuve (photo ou signature) et GPS si possible. */
export function DialogueLivree({ livraison }: { livraison: Livraison }) {
    const rafraichir = useRafraichirLivraisons();
    const [photoUrl, setPhotoUrl] = useState('');
    const [signatureUrl, setSignatureUrl] = useState('');
    const [reserve, setReserve] = useState('');
    const [commentaire, setCommentaire] = useState('');
    const [gps, setGps] = useState<{ latitude: number; longitude: number } | null>(null);
    const [localisation, setLocalisation] = useState(false);

    const localiser = () => {
        if (!navigator.geolocation) { toast.error('La localisation n\'est pas disponible sur cet appareil.'); return; }
        setLocalisation(true);
        navigator.geolocation.getCurrentPosition(
            p => { setGps({ latitude: Number(p.coords.latitude.toFixed(6)), longitude: Number(p.coords.longitude.toFixed(6)) }); setLocalisation(false); },
            () => { toast.error('Position introuvable : autorisez la localisation.'); setLocalisation(false); },
            { enableHighAccuracy: true, timeout: 15_000 },
        );
    };

    return (
        <DialogueAction
            declencheur={<Button size="sm"><CheckCircle2 /> Livrée</Button>}
            titre="Confirmer la livraison"
            description="Une photo ou une signature du client est obligatoire comme preuve."
            libelleConfirmer="Confirmer la livraison"
            onConfirmer={() => executer(api.post(`/livraisons/${livraison.id}/livree`, {
                photoUrl: photoUrl.trim() || null,
                signatureUrl: signatureUrl.trim() || null,
                latitude: gps?.latitude ?? null,
                longitude: gps?.longitude ?? null,
                commentaire: commentaire.trim() || null,
                reserve: reserve.trim() || null,
            }), reserve.trim() ? 'Livrée avec réserve.' : 'Livraison confirmée.', rafraichir)}
        >
            <div className="grid gap-3">
                <div className="grid gap-2">
                    <Label htmlFor="photo">Lien de la photo du colis remis</Label>
                    <Input id="photo" type="url" placeholder="https://..." value={photoUrl} onChange={e => setPhotoUrl(e.target.value)} />
                </div>
                <div className="grid gap-2">
                    <Label htmlFor="signature">Lien de la signature du client</Label>
                    <Input id="signature" type="url" placeholder="https://..." value={signatureUrl} onChange={e => setSignatureUrl(e.target.value)} />
                </div>
                <Button type="button" variant="outline" onClick={localiser} disabled={localisation}>
                    <LocateFixed /> {gps ? `Position : ${gps.latitude}, ${gps.longitude}` : localisation ? 'Localisation...' : 'Enregistrer ma position GPS'}
                </Button>
                <div className="grid gap-2">
                    <Label htmlFor="reserve">Réserve du client (colis abîmé, pièce manquante...)</Label>
                    <Textarea id="reserve" value={reserve} onChange={e => setReserve(e.target.value)} maxLength={1000} />
                    <p className="text-muted-foreground text-xs">Une réserve n'ouvre pas de SAV automatiquement.</p>
                </div>
                <div className="grid gap-2">
                    <Label htmlFor="commentaire">Commentaire</Label>
                    <Input id="commentaire" value={commentaire} onChange={e => setCommentaire(e.target.value)} maxLength={1000} />
                </div>
            </div>
        </DialogueAction>
    );
}

/** Échec : client absent (retour au dépôt, relivraison) ou refus (retour à contrôler, remboursement / avoir). */
export function DialogueEchec({ livraison }: { livraison: Livraison }) {
    const rafraichir = useRafraichirLivraisons();
    const [refus, setRefus] = useState(false);
    const [regularisation, setRegularisation] = useState<'Remboursement' | 'Avoir'>('Remboursement');
    const sav = !!livraison.ticketSAVId;

    return (
        <DialogueAction
            declencheur={<Button size="sm" variant="outline"><XCircle /> Échec</Button>}
            titre="Livraison impossible"
            champMotif="Que s'est-il passé ?"
            motifObligatoire
            libelleConfirmer="Enregistrer l'échec"
            destructif
            onConfirmer={motif => executer(
                api.post(`/livraisons/${livraison.id}/echec`, { motif, refusClient: refus, regularisation }),
                refus ? 'Refus enregistré : le colis doit revenir au dépôt pour contrôle.' : 'Échec enregistré : colis à ramener au dépôt, relivraison à planifier.',
                rafraichir,
            )}
        >
            <div className="grid gap-3">
                <Select value={refus ? 'refus' : 'absent'} onValueChange={v => setRefus(v === 'refus')}>
                    <SelectTrigger><SelectValue /></SelectTrigger>
                    <SelectContent>
                        <SelectItem value="absent">Client absent / injoignable (nouvelle tentative)</SelectItem>
                        <SelectItem value="refus">Le client refuse le colis</SelectItem>
                    </SelectContent>
                </Select>
                {refus && !sav && (
                    <div className="grid gap-2">
                        <Label>Le client souhaite</Label>
                        <Select value={regularisation} onValueChange={v => setRegularisation(v as 'Remboursement' | 'Avoir')}>
                            <SelectTrigger><SelectValue /></SelectTrigger>
                            <SelectContent>
                                <SelectItem value="Remboursement">Être remboursé</SelectItem>
                                <SelectItem value="Avoir">Un avoir pour une prochaine commande</SelectItem>
                            </SelectContent>
                        </Select>
                    </div>
                )}
            </div>
        </DialogueAction>
    );
}
