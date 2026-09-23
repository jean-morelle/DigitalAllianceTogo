import { useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { Banknote } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { DialogueAction } from '@/components/DialogueAction';
import { EnvoiFichier } from '@/components/Fichiers';
import { api } from '@/lib/api';
import { formatFcfa } from '@/lib/format';

/**
 * Le client a payé par T-Money / Flooz / virement et envoie sa preuve (souvent sur WhatsApp) :
 * le Commercial l'enregistre. Le paiement part ensuite dans « Paiements à vérifier ».
 */
export function DialoguePaiement({ commandeId, resteAPayer }: { commandeId: string; resteAPayer: number }) {
    const queryClient = useQueryClient();
    const [reference, setReference] = useState('');
    const [preuveUrl, setPreuveUrl] = useState<string | null>(null);

    return (
        <DialogueAction
            declencheur={<Button><Banknote /> Enregistrer un paiement</Button>}
            titre={`Paiement de ${formatFcfa(resteAPayer)}`}
            description="Le montant est toujours le reste à payer. Le paiement devra ensuite être vérifié et confirmé."
            libelleConfirmer="Enregistrer"
            onConfirmer={() => {
                if (!reference.trim()) {
                    toast.error('La référence de la transaction est obligatoire.');
                    return Promise.reject(new Error('Référence manquante'));
                }
                return api.post(`/commandes/${commandeId}/paiements`, { referenceExterne: reference.trim(), preuveUrl })
                    .then(() => {
                        toast.success('Paiement enregistré : à vérifier dans « Paiements ».');
                        setReference('');
                        setPreuveUrl(null);
                        for (const cle of ['commande', 'commandes', 'paiements', 'a-traiter']) void queryClient.invalidateQueries({ queryKey: [cle] });
                    })
                    .catch((e: Error) => { toast.error(e.message); throw e; });
            }}
        >
            <div className="grid gap-4">
                <div className="grid gap-2">
                    <Label htmlFor="refTx">Référence de la transaction *</Label>
                    <Input id="refTx" placeholder="Ex : identifiant T-Money / Flooz" value={reference} onChange={e => setReference(e.target.value)} maxLength={100} />
                </div>
                <div className="grid gap-2">
                    <Label>Capture ou reçu (facultatif)</Label>
                    <EnvoiFichier categorie="paiements" valeur={preuveUrl} surChangement={setPreuveUrl} libelle="Joindre la capture" />
                </div>
            </div>
        </DialogueAction>
    );
}
