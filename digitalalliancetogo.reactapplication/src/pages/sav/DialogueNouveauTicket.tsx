import { useState } from 'react';
import { useNavigate } from 'react-router';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { LifeBuoy, Search } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { DialogueAction } from '@/components/DialogueAction';
import { api } from '@/lib/api';
import { formatDate } from '@/lib/format';
import type { Commande, CommandeDetail, PaginatedList } from '@/lib/types';

const APRES_LIVRAISON = ['Livree', 'Cloturee'];

/**
 * Ouverture d'un ticket SAV (§26) : le client appelle, le Commercial retrouve la commande
 * (ou part d'une commande donnée), choisit le produit concerné et décrit la panne.
 */
export function DialogueNouveauTicket({ commandeId: commandeFixe, surCree }: { commandeId?: string; surCree?: (id: string) => void }) {
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const [recherche, setRecherche] = useState('');
    const [terme, setTerme] = useState('');
    const [commandeId, setCommandeId] = useState(commandeFixe ?? '');
    const [ligneId, setLigneId] = useState('');
    const [quantite, setQuantite] = useState(1);
    const [motif, setMotif] = useState('');

    const { data: resultats, isFetching } = useQuery({
        queryKey: ['commandes', 'sav', terme],
        queryFn: () => api.get<PaginatedList<Commande>>('/commandes', { recherche: terme, pageSize: 10 }),
        enabled: !commandeFixe && terme.length >= 3,
    });
    const { data: commande } = useQuery({
        queryKey: ['commande', commandeId],
        queryFn: () => api.get<CommandeDetail>(`/commandes/${commandeId}`),
        enabled: !!commandeId,
    });
    const livrees = resultats?.items.filter(c => APRES_LIVRAISON.includes(c.statut)) ?? [];
    const ligne = commande?.lignes.find(l => l.id === ligneId) ?? (commande?.lignes.length === 1 ? commande.lignes[0] : undefined);

    return (
        <DialogueAction
            declencheur={<Button variant={commandeFixe ? 'outline' : 'default'}><LifeBuoy /> Ouvrir un ticket SAV</Button>}
            titre="Nouveau ticket SAV"
            description="Uniquement pour un produit livré. La commande n'est pas rouverte."
            libelleConfirmer="Ouvrir le ticket"
            onConfirmer={() => {
                if (!ligne || !motif.trim()) {
                    toast.error('Choisissez le produit et décrivez le problème.');
                    return Promise.reject(new Error('Formulaire incomplet'));
                }
                return api.post<string>('/sav', { ligneCommandeId: ligne.id, quantite, motif: motif.trim() })
                    .then(id => {
                        toast.success('Ticket ouvert : un technicien va diagnostiquer le produit.');
                        void queryClient.invalidateQueries({ queryKey: ['sav'] });
                        if (surCree) surCree(id); else navigate(`/sav/${id}`);
                    })
                    .catch((e: Error) => { toast.error(e.message); throw e; });
            }}
        >
            <div className="grid gap-4">
                {!commandeFixe && (
                    <div className="grid gap-2">
                        <Label>Commande du client</Label>
                        <form className="flex gap-2" onSubmit={e => { e.preventDefault(); setTerme(recherche.trim()); }}>
                            <Input placeholder="Référence CMD-… ou code client" value={recherche} onChange={e => setRecherche(e.target.value)} />
                            <Button type="submit" variant="outline" size="icon" aria-label="Rechercher"><Search /></Button>
                        </form>
                        {terme && !isFetching && livrees.length === 0 && (
                            <p className="text-muted-foreground text-xs">Aucune commande livrée ne correspond.</p>
                        )}
                        {livrees.length > 0 && (
                            <Select value={commandeId} onValueChange={v => { setCommandeId(v); setLigneId(''); }}>
                                <SelectTrigger><SelectValue placeholder="Choisir la commande" /></SelectTrigger>
                                <SelectContent>
                                    {livrees.map(c => <SelectItem key={c.id} value={c.id}>{c.reference} · {c.codeClient} · {formatDate(c.dateCreation, false)}</SelectItem>)}
                                </SelectContent>
                            </Select>
                        )}
                    </div>
                )}

                {commande && (
                    <>
                        <div className="grid gap-2">
                            <Label>Produit concerné</Label>
                            <Select value={ligne?.id ?? ''} onValueChange={v => { setLigneId(v); setQuantite(1); }}>
                                <SelectTrigger><SelectValue placeholder="Choisir le produit" /></SelectTrigger>
                                <SelectContent>
                                    {commande.lignes.map(l => <SelectItem key={l.id} value={l.id}>{l.produitNom} ({l.quantite} commandé{l.quantite > 1 ? 's' : ''})</SelectItem>)}
                                </SelectContent>
                            </Select>
                        </div>
                        {ligne && ligne.quantite > 1 && (
                            <div className="grid gap-2">
                                <Label htmlFor="qte">Nombre d'unités concernées</Label>
                                <Input id="qte" type="number" min={1} max={ligne.quantite} value={quantite}
                                    onChange={e => setQuantite(Math.min(ligne.quantite, Math.max(1, Math.floor(Number(e.target.value) || 1))))} />
                            </div>
                        )}
                    </>
                )}

                <div className="grid gap-2">
                    <Label htmlFor="panne">Problème décrit par le client *</Label>
                    <Textarea id="panne" placeholder="Ex : l'écran reste noir au démarrage" value={motif} onChange={e => setMotif(e.target.value)} maxLength={1000} />
                </div>
            </div>
        </DialogueAction>
    );
}
