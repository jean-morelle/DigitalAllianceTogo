import { useState, type FormEvent, type ReactNode } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Loader2, Save } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Chargement, EnTetePage, EtatErreur } from '@/components/commun';
import { api } from '@/lib/api';
import { formatDate } from '@/lib/format';
import type { Parametres } from '@/lib/types';

function Champ({ id, libelle, aide, children }: { id: string; libelle: string; aide?: string; children: ReactNode }) {
    return (
        <div className="grid gap-2">
            <Label htmlFor={id}>{libelle}</Label>
            {children}
            {aide && <p className="text-muted-foreground text-xs">{aide}</p>}
        </div>
    );
}

/** Paramètres de l'entreprise (Administrateur) : numéros de paiement et seuils métier. */
export function ParametresPage() {
    const { data, isPending, error } = useQuery({ queryKey: ['parametres'], queryFn: () => api.get<Parametres>('/parametres') });
    if (isPending) return <Chargement />;
    if (error) return <EtatErreur erreur={error} />;
    // key : le formulaire repart des valeurs enregistrées après chaque sauvegarde
    return <Formulaire key={data.dateModification} initial={data} />;
}

function Formulaire({ initial }: { initial: Parametres }) {
    const queryClient = useQueryClient();
    const [p, setP] = useState(initial);
    const texte = (cle: 'numeroTMoney' | 'numeroFlooz' | 'nomBeneficiairePaiement') => ({
        id: cle, value: p[cle] ?? '', onChange: (e: React.ChangeEvent<HTMLInputElement>) => setP({ ...p, [cle]: e.target.value }),
    });
    const nombre = (cle: 'seuilRemiseCommercialPourcent' | 'seuilAugmentationModificationPourcent' | 'delaiExpirationPaiementHeures' | 'dureeValiditeDevisJours') => ({
        id: cle, type: 'number', min: 0, value: p[cle], onChange: (e: React.ChangeEvent<HTMLInputElement>) => setP({ ...p, [cle]: Number(e.target.value) }),
    });

    const enregistrer = useMutation({
        mutationFn: () => api.put('/parametres', p),
        onSuccess: () => { toast.success('Paramètres enregistrés.'); void queryClient.invalidateQueries({ queryKey: ['parametres'] }); },
        onError: e => toast.error(e.message),
    });
    const soumettre = (e: FormEvent) => { e.preventDefault(); enregistrer.mutate(); };

    return (
        <form onSubmit={soumettre}>
            <EnTetePage titre="Paramètres" description={`Dernière modification : ${formatDate(initial.dateModification)}`}
                actions={<Button type="submit" disabled={enregistrer.isPending}>{enregistrer.isPending ? <Loader2 className="animate-spin" /> : <Save />} Enregistrer</Button>} />
            <div className="grid gap-6 lg:grid-cols-2">
                <Card>
                    <CardHeader>
                        <CardTitle className="text-base">Paiement Mobile Money</CardTitle>
                        <CardDescription>Affichés au client au moment de payer sa commande.</CardDescription>
                    </CardHeader>
                    <CardContent className="grid gap-4">
                        <Champ id="numeroTMoney" libelle="Numéro T-Money"><Input placeholder="+228 90 00 00 00" {...texte('numeroTMoney')} /></Champ>
                        <Champ id="numeroFlooz" libelle="Numéro Flooz"><Input placeholder="+228 99 00 00 00" {...texte('numeroFlooz')} /></Champ>
                        <Champ id="nomBeneficiairePaiement" libelle="Nom du bénéficiaire" aide="Le nom que l'opérateur affiche au client avant qu'il valide le transfert.">
                            <Input maxLength={100} {...texte('nomBeneficiairePaiement')} />
                        </Champ>
                    </CardContent>
                </Card>
                <Card>
                    <CardHeader>
                        <CardTitle className="text-base">Règles métier</CardTitle>
                        <CardDescription>Au-delà de ces seuils, la validation de l'Administrateur est obligatoire.</CardDescription>
                    </CardHeader>
                    <CardContent className="grid gap-4 sm:grid-cols-2">
                        <Champ id="seuilRemiseCommercialPourcent" libelle="Remise max. du Commercial (%)"><Input max={100} step="0.01" {...nombre('seuilRemiseCommercialPourcent')} /></Champ>
                        <Champ id="seuilAugmentationModificationPourcent" libelle="Hausse max. sur modification (%)"><Input max={100} step="0.01" {...nombre('seuilAugmentationModificationPourcent')} /></Champ>
                        <Champ id="delaiExpirationPaiementHeures" libelle="Délai de paiement (heures)" aide="Passé ce délai, la commande impayée est annulée.">
                            <Input max={720} {...nombre('delaiExpirationPaiementHeures')} />
                        </Champ>
                        <Champ id="dureeValiditeDevisJours" libelle="Validité d'un devis (jours)"><Input max={365} {...nombre('dureeValiditeDevisJours')} /></Champ>
                    </CardContent>
                </Card>
            </div>
        </form>
    );
}
