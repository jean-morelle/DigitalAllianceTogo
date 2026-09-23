import { useState, type ReactNode } from 'react';
import { Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
    Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger,
} from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';

interface Props {
    declencheur: ReactNode;
    titre: string;
    description?: string;
    /** Libellé du champ texte ; absent = simple confirmation. */
    champMotif?: string;
    motifObligatoire?: boolean;
    libelleConfirmer: string;
    destructif?: boolean;
    /** Contenu additionnel (ex : choix remboursement / avoir). */
    children?: ReactNode;
    onConfirmer: (motif: string) => Promise<unknown>;
}

/**
 * Dialogue de confirmation d'une action métier, avec motif facultatif ou obligatoire
 * (rejet d'un paiement, annulation, clôture exceptionnelle...). Reste ouvert en cas d'erreur.
 */
export function DialogueAction({
    declencheur, titre, description, champMotif, motifObligatoire, libelleConfirmer, destructif, children, onConfirmer,
}: Props) {
    const [ouvert, setOuvert] = useState(false);
    const [motif, setMotif] = useState('');
    const [enCours, setEnCours] = useState(false);

    const confirmer = async () => {
        setEnCours(true);
        try {
            await onConfirmer(motif.trim());
            setOuvert(false);
            setMotif('');
        } catch {
            /* l'erreur est affichée par l'appelant (toast) */
        } finally {
            setEnCours(false);
        }
    };

    return (
        <Dialog open={ouvert} onOpenChange={setOuvert}>
            <DialogTrigger asChild>{declencheur}</DialogTrigger>
            <DialogContent>
                <DialogHeader>
                    <DialogTitle>{titre}</DialogTitle>
                    {description && <DialogDescription>{description}</DialogDescription>}
                </DialogHeader>
                {children}
                {champMotif && (
                    <div className="grid gap-2">
                        <Label htmlFor="motif">{champMotif}{motifObligatoire && ' *'}</Label>
                        <Textarea id="motif" value={motif} onChange={e => setMotif(e.target.value)} maxLength={500} />
                    </div>
                )}
                <DialogFooter>
                    <Button variant="outline" onClick={() => setOuvert(false)} disabled={enCours}>Annuler</Button>
                    <Button
                        variant={destructif ? 'destructive' : 'default'}
                        onClick={confirmer}
                        disabled={enCours || (motifObligatoire && !motif.trim())}
                    >
                        {enCours && <Loader2 className="animate-spin" />}
                        {libelleConfirmer}
                    </Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    );
}
