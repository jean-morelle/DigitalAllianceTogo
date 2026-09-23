import { useState, type ChangeEvent, type FormEvent } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Loader2, MapPin, Plus, Trash2 } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Chargement, EtatErreur } from '@/components/commun';
import { api } from '@/lib/api';
import type { Adresse } from '@/lib/types';
import { adresseParDefaut, useMaFiche } from './client';
import { cn } from '@/lib/utils';

function FormulaireAdresse({ clientId, surCree, surAnnuler }: { clientId: string; surCree: (a: Adresse) => void; surAnnuler?: () => void }) {
    const queryClient = useQueryClient();
    const [a, setA] = useState({ libelle: 'Maison', ligne1: '', ligne2: '', ville: 'Lomé', codePostal: '' });
    const creer = useMutation({
        mutationFn: () => api.post<string>(`/clients/${clientId}/adresses`, {
            libelle: a.libelle.trim(), ligne1: a.ligne1.trim(), ligne2: a.ligne2.trim() || null, ville: a.ville.trim(), pays: 'Togo', codePostal: a.codePostal.trim() || null,
        }),
        onSuccess: async id => {
            await queryClient.invalidateQueries({ queryKey: ['ma-fiche'] });
            toast.success('Adresse enregistrée.');
            surCree({ id, libelle: a.libelle, ligne1: a.ligne1, ligne2: a.ligne2 || null, ville: a.ville, pays: 'Togo', codePostal: a.codePostal });
        },
        onError: e => toast.error(e.message),
    });
    const soumettre = (e: FormEvent) => { e.preventDefault(); creer.mutate(); };
    const champ = (cle: keyof typeof a) => ({ value: a[cle], onChange: (e: ChangeEvent<HTMLInputElement>) => setA(s => ({ ...s, [cle]: e.target.value })) });

    return (
        <form onSubmit={soumettre} className="grid gap-3 rounded-md border p-3">
            <div className="grid gap-3 sm:grid-cols-2">
                <div className="grid gap-1.5"><Label htmlFor="libelle">Nom de l'adresse</Label><Input id="libelle" placeholder="Maison, Bureau..." required {...champ('libelle')} /></div>
                <div className="grid gap-1.5"><Label htmlFor="ville">Ville</Label><Input id="ville" required {...champ('ville')} /></div>
            </div>
            <div className="grid gap-1.5"><Label htmlFor="ligne1">Quartier, rue, repère *</Label><Input id="ligne1" placeholder="Ex : Bè Kpota, rue de la pharmacie" required {...champ('ligne1')} /></div>
            <div className="grid gap-1.5"><Label htmlFor="ligne2">Complément (facultatif)</Label><Input id="ligne2" placeholder="Portail bleu, 2e étage..." {...champ('ligne2')} /></div>
            <div className="flex justify-end gap-2">
                {surAnnuler && <Button type="button" variant="ghost" onClick={surAnnuler}>Annuler</Button>}
                <Button type="submit" disabled={creer.isPending}>{creer.isPending && <Loader2 className="animate-spin" />} Enregistrer l'adresse</Button>
            </div>
        </form>
    );
}

/** Choix d'une adresse de livraison (la première par défaut), avec ajout sur place si besoin. */
export function ChoixAdresse({ valeur, surChangement }: { valeur: string; surChangement: (id: string) => void }) {
    const { data: fiche, isPending, error } = useMaFiche();
    const [ajout, setAjout] = useState(false);

    if (isPending) return <Chargement lignes={2} />;
    if (error) return <EtatErreur erreur={error} />;

    const adresses = fiche.adresses;
    const selection = adresseParDefaut(adresses, valeur);

    return (
        <div className="grid gap-2">
            {adresses.map(a => (
                <button key={a.id} type="button" onClick={() => surChangement(a.id)}
                    className={cn('flex items-start gap-3 rounded-md border p-3 text-left text-sm', a.id === selection && 'border-primary ring-primary/30 ring-2')}>
                    <MapPin className="text-muted-foreground mt-0.5 size-4 shrink-0" />
                    <span><span className="font-medium">{a.libelle}</span><br />{a.ligne1}{a.ligne2 && `, ${a.ligne2}`}, {a.ville}</span>
                </button>
            ))}
            {ajout || adresses.length === 0
                ? <FormulaireAdresse clientId={fiche.id} surCree={a => { setAjout(false); surChangement(a.id); }} surAnnuler={adresses.length ? () => setAjout(false) : undefined} />
                : <Button type="button" variant="outline" onClick={() => setAjout(true)}><Plus /> Nouvelle adresse</Button>}
        </div>
    );
}

/** Page « Mes adresses ». */
export function MesAdresses() {
    const queryClient = useQueryClient();
    const { data: fiche, isPending, error } = useMaFiche();
    const [ajout, setAjout] = useState(false);

    const supprimer = useMutation({
        mutationFn: (id: string) => api.delete(`/clients/${fiche!.id}/adresses/${id}`),
        onSuccess: () => { toast.success('Adresse supprimée.'); void queryClient.invalidateQueries({ queryKey: ['ma-fiche'] }); },
        onError: e => toast.error(e.message),
    });

    if (isPending) return <Chargement />;
    if (error) return <EtatErreur erreur={error} />;

    return (
        <Card>
            <CardHeader className="flex flex-row items-center justify-between">
                <CardTitle className="text-base">Mes adresses de livraison</CardTitle>
                {!ajout && <Button size="sm" onClick={() => setAjout(true)}><Plus /> Ajouter</Button>}
            </CardHeader>
            <CardContent className="grid gap-3">
                {ajout && <FormulaireAdresse clientId={fiche.id} surCree={() => setAjout(false)} surAnnuler={() => setAjout(false)} />}
                {fiche.adresses.length === 0 && !ajout && <p className="text-muted-foreground text-sm">Aucune adresse : ajoutez-en une pour être livré.</p>}
                {fiche.adresses.map(a => (
                    <div key={a.id} className="flex items-start justify-between gap-3 rounded-md border p-3 text-sm">
                        <span><span className="font-medium">{a.libelle}</span><br />{a.ligne1}{a.ligne2 && `, ${a.ligne2}`}, {a.ville}</span>
                        <Button variant="ghost" size="icon" aria-label={`Supprimer ${a.libelle}`} onClick={() => supprimer.mutate(a.id)}><Trash2 /></Button>
                    </div>
                ))}
                <p className="text-muted-foreground text-xs">Téléphone de contact pour la livraison : {fiche.telephone}</p>
            </CardContent>
        </Card>
    );
}
