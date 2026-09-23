import { useRef, useState, type FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, ExternalLink, ImagePlus, Loader2, Plus, Save, Star, Trash2, X } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { Chargement, EnTetePage, EtatErreur } from '@/components/commun';
import { DialogueAction } from '@/components/DialogueAction';
import { api } from '@/lib/api';
import { useAuth } from '@/lib/auth';
import { Roles } from '@/lib/roles';
import type { Categorie, ProduitDetail } from '@/lib/types';
import { cn } from '@/lib/utils';

interface Saisie {
    reference: string;
    nom: string;
    description: string;
    prix: string;
    categorieId: string;
    marqueId: string;
    actif: boolean;
}

const VIDE: Saisie = { reference: '', nom: '', description: '', prix: '', categorieId: '', marqueId: '', actif: true };

/** Création (/catalogue/nouveau) ou fiche d'un produit (/catalogue/:id) : infos, photos, caractéristiques. */
export function ProduitFormPage() {
    const { id } = useParams();
    const { data, isPending, error } = useQuery({
        queryKey: ['produit', id],
        queryFn: () => api.get<ProduitDetail>(`/produits/${id}`),
        enabled: !!id,
    });

    if (!id) return <Fiche produit={null} />;
    if (isPending) return <Chargement />;
    if (error) return <EtatErreur erreur={error} />;
    return <Fiche key={data.id} produit={data} />;
}

function Fiche({ produit }: { produit: ProduitDetail | null }) {
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const { aRole } = useAuth();
    const [s, setS] = useState<Saisie>(produit
        ? { reference: produit.reference, nom: produit.nom, description: produit.description, prix: String(produit.prix), categorieId: produit.categorieId, marqueId: produit.marqueId, actif: produit.actif }
        : VIDE);
    const { data: categories } = useQuery({ queryKey: ['categories'], queryFn: () => api.get<Categorie[]>('/categories') });
    const { data: marques } = useQuery({ queryKey: ['marques'], queryFn: () => api.get<Categorie[]>('/marques') });

    const rafraichir = () => {
        for (const cle of ['produit', 'catalogue-gestion', 'catalogue']) void queryClient.invalidateQueries({ queryKey: [cle] });
    };

    const enregistrer = useMutation({
        mutationFn: async () => {
            const corps = { nom: s.nom.trim(), description: s.description.trim(), prix: Number(s.prix), categorieId: s.categorieId, marqueId: s.marqueId };
            if (produit) {
                await api.put(`/produits/${produit.id}`, { ...corps, actif: s.actif });
                return produit.id;
            }
            return api.post<string>('/produits', { ...corps, reference: s.reference.trim() });
        },
        onSuccess: nouvelId => {
            rafraichir();
            if (produit) toast.success('Produit enregistré.');
            else { toast.success('Produit créé : ajoutez maintenant ses photos.'); navigate(`/catalogue/${nouvelId}`, { replace: true }); }
        },
        onError: e => toast.error(e.message),
    });

    const soumettre = (e: FormEvent) => { e.preventDefault(); enregistrer.mutate(); };
    const valide = s.nom.trim() && Number(s.prix) > 0 && s.categorieId && s.marqueId && (produit || s.reference.trim());
    // Retirés : proposés seulement s'ils sont déjà ceux du produit
    const choix = (liste: Categorie[] | undefined, actuel: string) => liste?.filter(e => e.actif !== false || e.id === actuel) ?? [];

    return (
        <>
            <Button asChild variant="ghost" size="sm" className="mb-2 -ml-2"><Link to="/catalogue"><ArrowLeft /> Catalogue</Link></Button>
            <EnTetePage titre={produit ? produit.nom : 'Nouveau produit'} description={produit?.reference}
                actions={produit && produit.actif && (
                    <Button asChild variant="outline"><a href={`/boutique/produits/${produit.id}`} target="_blank" rel="noreferrer"><ExternalLink /> Voir dans la boutique</a></Button>
                )} />

            <div className="grid gap-6 lg:grid-cols-[1fr_24rem]">
                <form onSubmit={soumettre}>
                    <Card>
                        <CardHeader><CardTitle className="text-base">Informations</CardTitle></CardHeader>
                        <CardContent className="grid gap-4 sm:grid-cols-2">
                            <div className="grid gap-2">
                                <Label htmlFor="reference">Référence</Label>
                                <Input id="reference" required disabled={!!produit} maxLength={50} placeholder="Ex : HP-250-G9-I5"
                                    value={s.reference} onChange={e => setS({ ...s, reference: e.target.value })} />
                                {produit && <p className="text-muted-foreground text-xs">Non modifiable : elle figure sur les devis et commandes.</p>}
                            </div>
                            <div className="grid gap-2">
                                <Label htmlFor="prix">Prix (FCFA)</Label>
                                <Input id="prix" type="number" required min={1} step={1} value={s.prix} onChange={e => setS({ ...s, prix: e.target.value })} />
                                {produit && <p className="text-muted-foreground text-xs">Les devis et commandes déjà faits gardent leur prix.</p>}
                            </div>
                            <div className="grid gap-2 sm:col-span-2">
                                <Label htmlFor="nom">Nom</Label>
                                <Input id="nom" required maxLength={200} value={s.nom} onChange={e => setS({ ...s, nom: e.target.value })} />
                            </div>
                            <div className="grid gap-2">
                                <Label>Catégorie</Label>
                                <Select value={s.categorieId} onValueChange={v => setS({ ...s, categorieId: v })}>
                                    <SelectTrigger><SelectValue placeholder="Choisir" /></SelectTrigger>
                                    <SelectContent>{choix(categories, s.categorieId).map(c => <SelectItem key={c.id} value={c.id}>{c.nom}</SelectItem>)}</SelectContent>
                                </Select>
                            </div>
                            <div className="grid gap-2">
                                <Label>Marque</Label>
                                <Select value={s.marqueId} onValueChange={v => setS({ ...s, marqueId: v })}>
                                    <SelectTrigger><SelectValue placeholder="Choisir" /></SelectTrigger>
                                    <SelectContent>{choix(marques, s.marqueId).map(m => <SelectItem key={m.id} value={m.id}>{m.nom}</SelectItem>)}</SelectContent>
                                </Select>
                            </div>
                            <div className="grid gap-2 sm:col-span-2">
                                <Label htmlFor="description">Description</Label>
                                <Textarea id="description" rows={6} maxLength={4000} value={s.description} onChange={e => setS({ ...s, description: e.target.value })} />
                            </div>
                            {produit && (
                                <label className="flex items-center gap-2 text-sm sm:col-span-2">
                                    <input type="checkbox" className="size-4" checked={s.actif} onChange={e => setS({ ...s, actif: e.target.checked })} />
                                    En vente dans la boutique
                                </label>
                            )}
                            <div className="flex flex-wrap gap-2 sm:col-span-2">
                                <Button type="submit" disabled={!valide || enregistrer.isPending}>
                                    {enregistrer.isPending ? <Loader2 className="animate-spin" /> : <Save />} {produit ? 'Enregistrer' : 'Créer le produit'}
                                </Button>
                                {produit && aRole(Roles.Admin) && (
                                    <DialogueAction declencheur={<Button type="button" variant="ghost" className="text-destructive ml-auto"><Trash2 /> Supprimer</Button>}
                                        titre="Supprimer ce produit ?" destructif libelleConfirmer="Supprimer"
                                        description="Possible seulement s'il n'a jamais été stocké ni vendu ; sinon, décochez « En vente »."
                                        onConfirmer={() => api.delete(`/produits/${produit.id}`)
                                            .then(() => { toast.success('Produit supprimé.'); rafraichir(); navigate('/catalogue', { replace: true }); })
                                            .catch((e: Error) => { toast.error(e.message); throw e; })} />
                                )}
                            </div>
                        </CardContent>
                    </Card>
                </form>

                {produit && (
                    <div className="grid content-start gap-6">
                        <Photos produit={produit} surChangement={rafraichir} />
                        <Caracteristiques produit={produit} surChangement={rafraichir} />
                    </div>
                )}
            </div>
        </>
    );
}

function Photos({ produit, surChangement }: { produit: ProduitDetail; surChangement: () => void }) {
    const entree = useRef<HTMLInputElement>(null);
    const [envoi, setEnvoi] = useState(false);
    const images = [...produit.images].sort((a, b) => a.ordre - b.ordre);

    const ajouter = async (fichiers: FileList | null) => {
        if (!fichiers?.length) return;
        setEnvoi(true);
        try {
            for (const fichier of Array.from(fichiers)) {
                if (fichier.size > 5 * 1024 * 1024) { toast.error(`${fichier.name} dépasse 5 Mo.`); continue; }
                const { url } = await api.envoyerFichier('produits', fichier, fichier.name);
                await api.post(`/produits/${produit.id}/images`, { url, ordre: 0, estPrincipale: false });
            }
        } catch (e) {
            toast.error(e instanceof Error ? e.message : 'Envoi impossible.');
        } finally {
            setEnvoi(false);
            if (entree.current) entree.current.value = '';
            surChangement();
        }
    };

    const action = (promesse: Promise<unknown>) => promesse.then(surChangement, (e: Error) => toast.error(e.message));

    return (
        <Card>
            <CardHeader>
                <CardTitle className="text-base">Photos</CardTitle>
                <CardDescription>L'étoile désigne la vignette de la boutique. JPEG, PNG ou WebP, 5 Mo max.</CardDescription>
            </CardHeader>
            <CardContent className="grid gap-3">
                {images.length > 0 && (
                    <div className="grid grid-cols-3 gap-2">
                        {images.map(i => (
                            <div key={i.id} className={cn('group relative aspect-square overflow-hidden rounded-md border bg-white', i.estPrincipale && 'ring-primary ring-2')}>
                                <img src={i.url} alt="" className="size-full object-contain" />
                                <div className="absolute inset-x-0 bottom-0 flex justify-between bg-black/50 p-1">
                                    <button type="button" aria-label="Photo principale" title="Photo principale" className="text-white disabled:opacity-100"
                                        disabled={i.estPrincipale} onClick={() => action(api.put(`/produits/images/${i.id}/principale`, {}))}>
                                        <Star className={cn('size-4', i.estPrincipale && 'fill-yellow-400 text-yellow-400')} />
                                    </button>
                                    <button type="button" aria-label="Supprimer la photo" title="Supprimer" className="text-white"
                                        onClick={() => action(api.delete(`/produits/images/${i.id}`))}>
                                        <X className="size-4" />
                                    </button>
                                </div>
                            </div>
                        ))}
                    </div>
                )}
                <input ref={entree} type="file" multiple className="hidden" accept="image/jpeg,image/png,image/webp" onChange={e => void ajouter(e.target.files)} />
                <Button type="button" variant="outline" disabled={envoi} onClick={() => entree.current?.click()}>
                    {envoi ? <Loader2 className="animate-spin" /> : <ImagePlus />} {envoi ? 'Envoi...' : 'Ajouter des photos'}
                </Button>
            </CardContent>
        </Card>
    );
}

function Caracteristiques({ produit, surChangement }: { produit: ProduitDetail; surChangement: () => void }) {
    const [cle, setCle] = useState('');
    const [valeur, setValeur] = useState('');
    const attributs = [...produit.attributs].sort((a, b) => a.ordre - b.ordre);

    const ajouter = useMutation({
        mutationFn: () => api.post(`/produits/${produit.id}/attributs`, {
            cle: cle.trim(), valeur: valeur.trim(), ordre: attributs.reduce((max, a) => Math.max(max, a.ordre), 0) + 1,
        }),
        onSuccess: () => { setCle(''); setValeur(''); surChangement(); },
        onError: e => toast.error(e.message),
    });

    return (
        <Card>
            <CardHeader>
                <CardTitle className="text-base">Caractéristiques</CardTitle>
                <CardDescription>Ex : Processeur = Intel Core i5, Mémoire = 8 Go.</CardDescription>
            </CardHeader>
            <CardContent className="grid gap-3">
                {attributs.length > 0 && (
                    <div className="divide-y rounded-md border">
                        {attributs.map(a => (
                            <div key={a.id} className="flex items-center gap-2 px-3 py-1.5 text-sm">
                                <span className="text-muted-foreground w-28 shrink-0">{a.cle}</span>
                                <span className="flex-1">{a.valeur}</span>
                                <Button variant="ghost" size="icon" className="size-7" aria-label={`Retirer ${a.cle}`}
                                    onClick={() => api.delete(`/produits/attributs/${a.id}`).then(surChangement, (e: Error) => toast.error(e.message))}><X /></Button>
                            </div>
                        ))}
                    </div>
                )}
                <form className="flex gap-2" onSubmit={e => { e.preventDefault(); ajouter.mutate(); }}>
                    <Input placeholder="Nom" aria-label="Nom de la caractéristique" className="w-28" value={cle} onChange={e => setCle(e.target.value)} />
                    <Input placeholder="Valeur" aria-label="Valeur" value={valeur} onChange={e => setValeur(e.target.value)} />
                    <Button type="submit" size="icon" aria-label="Ajouter" disabled={!cle.trim() || !valeur.trim() || ajouter.isPending}><Plus /></Button>
                </form>
            </CardContent>
        </Card>
    );
}
