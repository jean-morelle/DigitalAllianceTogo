import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, Loader2 } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Chargement, EnTetePage, EtatErreur } from '@/components/commun';
import { EditeurLignes } from '@/components/EditeurLignes';
import { calculerTotaux, type LigneEdition } from '@/lib/lignes';
import { api } from '@/lib/api';
import { formatFcfa } from '@/lib/format';
import type { CommandeDetail } from '@/lib/types';

interface ResultatProposition {
    statut: string;
    message: string;
}

function Formulaire({ commande }: { commande: CommandeDetail }) {
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    // Produits déjà commandés : prix de la commande conservé (règle appliquée aussi par l'API)
    const [lignes, setLignes] = useState<LigneEdition[]>(() => commande.lignes.map(l => ({
        produitId: l.produitId, nom: l.produitNom, reference: l.produitReference, prixUnitaire: l.prixUnitaire, quantite: l.quantite, remise: l.remise,
    })));
    const [remiseGlobale, setRemiseGlobale] = useState(() => commande.remise - commande.lignes.reduce((s, l) => s + l.remise, 0));
    const [motif, setMotif] = useState('');
    const { total, remise, sousTotal } = calculerTotaux(lignes, remiseGlobale);

    const proposer = useMutation({
        mutationFn: () => api.post<ResultatProposition>(`/commandes/${commande.id}/modifications`, {
            lignes: lignes.map(({ produitId, quantite, remise }) => ({ produitId, quantite, remise })),
            remiseGlobale,
            motif: motif.trim(),
        }),
        onSuccess: r => {
            toast.success(r.message);
            for (const cle of ['commande', 'commandes', 'a-traiter']) void queryClient.invalidateQueries({ queryKey: [cle] });
            navigate(`/commandes/${commande.id}`);
        },
        onError: e => toast.error(e.message),
    });

    return (
        <div className="space-y-6">
            <Card>
                <CardHeader>
                    <CardTitle className="text-base">Nouvelle version</CardTitle>
                    <CardDescription>
                        Version actuelle : {formatFcfa(commande.total)}. Les produits déjà commandés gardent leur prix ; un nouveau produit prend le prix du catalogue.
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    <EditeurLignes lignes={lignes} remiseGlobale={remiseGlobale} totalReference={commande.total}
                        surChangement={(l, r) => { setLignes(l); setRemiseGlobale(r); }} />
                </CardContent>
            </Card>

            <Card>
                <CardContent className="grid gap-2 pt-6">
                    <Label htmlFor="motif">Motif de la modification *</Label>
                    <Textarea id="motif" placeholder="Ex : le client ajoute une souris et un sac" value={motif} onChange={e => setMotif(e.target.value)} maxLength={1000} />
                    <p className="text-muted-foreground text-xs">
                        {total > commande.total
                            ? 'Hausse : au-delà du seuil, l\'Administrateur valide d\'abord ; le complément devra être payé avant de reprendre la commande.'
                            : total < commande.total
                                ? 'Baisse : le client choisira remboursement ou avoir à l\'acceptation, validé ensuite par l\'Administrateur.'
                                : 'Même prix : l\'accord du client suffit.'}
                    </p>
                </CardContent>
            </Card>

            <div className="flex justify-end gap-2">
                <Button variant="outline" asChild><Link to={`/commandes/${commande.id}`}>Annuler</Link></Button>
                <Button disabled={lignes.length === 0 || !motif.trim() || remise > sousTotal || proposer.isPending} onClick={() => proposer.mutate()}>
                    {proposer.isPending && <Loader2 className="animate-spin" />}
                    Proposer au client
                </Button>
            </div>
        </div>
    );
}

export function ProposerModificationPage() {
    const { id = '' } = useParams();
    const { data, isPending, error } = useQuery({
        queryKey: ['commande', id],
        queryFn: () => api.get<CommandeDetail>(`/commandes/${id}`),
    });

    return (
        <>
            <Button asChild variant="ghost" size="sm" className="mb-2 -ml-2">
                <Link to={`/commandes/${id}`}><ArrowLeft /> Retour à la commande</Link>
            </Button>
            <EnTetePage titre={`Modifier ${data?.reference ?? 'la commande'}`}
                description="La version actuelle et ses paiements restent intacts : le client doit accepter la nouvelle version." />
            {isPending ? <Chargement /> : error ? <EtatErreur erreur={error} /> : <Formulaire commande={data} />}
        </>
    );
}
