import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, Loader2 } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { Chargement, EnTetePage, EtatErreur } from '@/components/commun';
import { EditeurLignes } from '@/components/EditeurLignes';
import { calculerTotaux, type LigneEdition } from '@/lib/lignes';
import { SelecteurClient } from '@/components/Selecteurs';
import { api } from '@/lib/api';
import type { Client, DevisDetail } from '@/lib/types';

function Formulaire({ devis }: { devis?: DevisDetail }) {
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const [client, setClient] = useState<Client | null>(null);
    const [lignes, setLignes] = useState<LigneEdition[]>(() => devis?.lignes.map(l => ({
        produitId: l.produitId, nom: l.produitNom, reference: l.produitReference, prixUnitaire: l.prixUnitaire, quantite: l.quantite, remise: l.remise,
    })) ?? []);
    // La remise globale n'est pas stockée à part : elle se déduit du total des remises de lignes
    const [remiseGlobale, setRemiseGlobale] = useState(() => devis ? devis.remise - devis.lignes.reduce((s, l) => s + l.remise, 0) : 0);
    const { sousTotal, remise } = calculerTotaux(lignes, remiseGlobale);

    const enregistrer = useMutation({
        mutationFn: () => {
            const corps = { lignes: lignes.map(({ produitId, quantite, remise }) => ({ produitId, quantite, remise })), remiseGlobale };
            return devis
                ? api.put(`/devis/${devis.id}`, corps).then(() => devis.id)
                : api.post<string>('/devis', { ...corps, clientId: client!.id });
        },
        onSuccess: id => {
            toast.success(devis ? 'Devis modifié : il doit être revalidé.' : 'Devis créé en brouillon.');
            void queryClient.invalidateQueries({ queryKey: ['devis'] });
            navigate(`/devis/${id}`);
        },
        onError: e => toast.error(e.message),
    });

    const valide = (devis || client) && lignes.length > 0 && remise <= sousTotal && lignes.every(l => l.remise <= l.prixUnitaire * l.quantite);

    return (
        <div className="space-y-6">
            {!devis && (
                <Card>
                    <CardHeader><CardTitle className="text-base">Client</CardTitle></CardHeader>
                    <CardContent className="grid gap-2 sm:max-w-md">
                        <Label>Pour qui est ce devis ?</Label>
                        <SelecteurClient client={client} surChoix={setClient} />
                    </CardContent>
                </Card>
            )}

            <Card>
                <CardHeader><CardTitle className="text-base">Produits</CardTitle></CardHeader>
                <CardContent>
                    <EditeurLignes lignes={lignes} remiseGlobale={remiseGlobale}
                        surChangement={(l, r) => { setLignes(l); setRemiseGlobale(r); }} />
                    <p className="text-muted-foreground mt-3 text-xs">
                        Les prix sont repris du catalogue au moment de l'enregistrement.
                    </p>
                </CardContent>
            </Card>

            <div className="flex justify-end gap-2">
                <Button variant="outline" asChild><Link to={devis ? `/devis/${devis.id}` : '/devis'}>Annuler</Link></Button>
                <Button disabled={!valide || enregistrer.isPending} onClick={() => enregistrer.mutate()}>
                    {enregistrer.isPending && <Loader2 className="animate-spin" />}
                    {devis ? 'Enregistrer les modifications' : 'Créer le devis'}
                </Button>
            </div>
        </div>
    );
}

export function DevisFormPage() {
    const { id } = useParams();
    const { data: devis, isPending, error } = useQuery({
        queryKey: ['devis-detail', id],
        queryFn: () => api.get<DevisDetail>(`/devis/${id}`),
        enabled: !!id,
    });

    return (
        <>
            <Button asChild variant="ghost" size="sm" className="mb-2 -ml-2">
                <Link to={id ? `/devis/${id}` : '/devis'}><ArrowLeft /> {id ? 'Retour au devis' : 'Devis'}</Link>
            </Button>
            <EnTetePage titre={id ? `Modifier ${devis?.reference ?? 'le devis'}` : 'Nouveau devis'}
                description={id ? 'Toute modification annule la validation interne.' : undefined} />
            {!id ? <Formulaire /> : isPending ? <Chargement /> : error ? <EtatErreur erreur={error} /> : <Formulaire devis={devis} />}
        </>
    );
}
