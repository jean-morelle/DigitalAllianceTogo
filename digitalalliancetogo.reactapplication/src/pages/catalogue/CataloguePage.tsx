import { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ImageOff, Pencil, Plus, Search } from 'lucide-react';
import { toast } from 'sonner';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Textarea } from '@/components/ui/textarea';
import { Chargement, EnTetePage, EtatErreur, EtatVide } from '@/components/commun';
import { Pagination } from '@/components/Pagination';
import { api } from '@/lib/api';
import { formatFcfa } from '@/lib/format';
import type { Categorie, PaginatedList, ProduitCatalogue } from '@/lib/types';

const TOUS = 'tous';

/** Catalogue (rôle Catalogue et Admin) : produits, catégories et marques. */
export function CataloguePage() {
    const [params, setParams] = useSearchParams();
    const onglet = params.get('onglet') ?? 'produits';
    return (
        <>
            <EnTetePage titre="Catalogue" description="Ce que les clients voient dans la boutique."
                actions={<Button asChild><Link to="/catalogue/nouveau"><Plus /> Nouveau produit</Link></Button>} />
            <Tabs value={onglet} onValueChange={v => setParams(v === 'produits' ? {} : { onglet: v })}>
                <TabsList className="mb-4">
                    <TabsTrigger value="produits">Produits</TabsTrigger>
                    <TabsTrigger value="categories">Catégories</TabsTrigger>
                    <TabsTrigger value="marques">Marques</TabsTrigger>
                </TabsList>
                <TabsContent value="produits"><ListeProduits /></TabsContent>
                <TabsContent value="categories"><Referentiel type="categories" libelle="catégorie" /></TabsContent>
                <TabsContent value="marques"><Referentiel type="marques" libelle="marque" /></TabsContent>
            </Tabs>
        </>
    );
}

function ListeProduits() {
    const navigate = useNavigate();
    const [params, setParams] = useSearchParams();
    const [recherche, setRecherche] = useState(params.get('q') ?? '');
    const categorieId = params.get('categorie') ?? TOUS;
    const etat = params.get('etat') ?? TOUS;
    const page = Number(params.get('page') ?? 1);

    const { data: categories } = useQuery({ queryKey: ['categories'], queryFn: () => api.get<Categorie[]>('/categories') });
    const { data, isPending, error } = useQuery({
        queryKey: ['catalogue-gestion', params.get('q'), categorieId, etat, page],
        queryFn: () => api.get<PaginatedList<ProduitCatalogue>>('/produits', {
            recherche: params.get('q'),
            categorieId: categorieId === TOUS ? undefined : categorieId,
            actif: etat === TOUS ? undefined : etat === 'actifs',
            pageNumber: page,
            pageSize: 25,
        }),
    });

    const maj = (changements: Record<string, string | null>) => {
        const suivants = new URLSearchParams(params);
        for (const [cle, valeur] of Object.entries(changements)) {
            if (!valeur || valeur === TOUS) suivants.delete(cle); else suivants.set(cle, valeur);
        }
        if (!('page' in changements)) suivants.delete('page');
        setParams(suivants);
    };

    return (
        <div className="grid gap-4">
            <div className="flex flex-wrap gap-2">
                <form className="relative min-w-60 flex-1" onSubmit={e => { e.preventDefault(); maj({ q: recherche.trim() || null }); }}>
                    <Search className="text-muted-foreground absolute top-2.5 left-2.5 size-4" />
                    <Input className="pl-8" placeholder="Nom ou référence" value={recherche} onChange={e => setRecherche(e.target.value)} />
                </form>
                <Select value={categorieId} onValueChange={v => maj({ categorie: v })}>
                    <SelectTrigger className="w-48"><SelectValue /></SelectTrigger>
                    <SelectContent>
                        <SelectItem value={TOUS}>Toutes les catégories</SelectItem>
                        {categories?.map(c => <SelectItem key={c.id} value={c.id}>{c.nom}</SelectItem>)}
                    </SelectContent>
                </Select>
                <Select value={etat} onValueChange={v => maj({ etat: v })}>
                    <SelectTrigger className="w-40"><SelectValue /></SelectTrigger>
                    <SelectContent>
                        <SelectItem value={TOUS}>Tous</SelectItem>
                        <SelectItem value="actifs">En vente</SelectItem>
                        <SelectItem value="inactifs">Retirés</SelectItem>
                    </SelectContent>
                </Select>
            </div>

            {isPending ? <Chargement /> : error ? <EtatErreur erreur={error} /> : data.items.length === 0 ? <EtatVide message="Aucun produit." /> : (
                <Card>
                    <CardContent className="p-0">
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    <TableHead className="w-16" />
                                    <TableHead>Produit</TableHead>
                                    <TableHead className="hidden md:table-cell">Catégorie · Marque</TableHead>
                                    <TableHead className="text-right">Prix</TableHead>
                                    <TableHead>État</TableHead>
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                {data.items.map(p => (
                                    <TableRow key={p.id} className="cursor-pointer" onClick={() => navigate(`/catalogue/${p.id}`)}>
                                        <TableCell>
                                            {p.imagePrincipaleUrl
                                                ? <img src={p.imagePrincipaleUrl} alt="" className="size-10 rounded bg-white object-contain" />
                                                : <div className="bg-muted flex size-10 items-center justify-center rounded" title="Pas de photo"><ImageOff className="text-muted-foreground size-4" /></div>}
                                        </TableCell>
                                        <TableCell>
                                            <Link to={`/catalogue/${p.id}`} className="font-medium hover:underline" onClick={e => e.stopPropagation()}>{p.nom}</Link>
                                            <div className="text-muted-foreground text-xs">{p.reference}</div>
                                        </TableCell>
                                        <TableCell className="hidden text-sm md:table-cell">{p.categorieNom} · {p.marqueNom}</TableCell>
                                        <TableCell className="text-right tabular-nums">{formatFcfa(p.prix)}</TableCell>
                                        <TableCell>{p.actif ? <Badge variant="secondary">En vente</Badge> : <Badge variant="outline">Retiré</Badge>}</TableCell>
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                    </CardContent>
                </Card>
            )}
            {data && <Pagination page={data.pageNumber} totalPages={data.totalPages} surChangement={p => maj({ page: String(p) })} />}
        </div>
    );
}

interface ElementReferentiel { id: string; nom: string; description: string; actif: boolean }
interface Edition { id: string | null; nom: string; description: string; actif: boolean }

/** Catégories ou marques : ajout, renommage, retrait de la vente (masque leurs produits de la boutique). */
function Referentiel({ type, libelle }: { type: 'categories' | 'marques'; libelle: string }) {
    const queryClient = useQueryClient();
    const [edition, setEdition] = useState<Edition | null>(null);
    const { data, isPending, error } = useQuery({ queryKey: [type], queryFn: () => api.get<ElementReferentiel[]>(`/${type}`) });

    const enregistrer = useMutation({
        mutationFn: (e: Edition) => e.id
            ? api.put(`/${type}/${e.id}`, { nom: e.nom.trim(), description: e.description.trim(), actif: e.actif })
            : api.post(`/${type}`, { nom: e.nom.trim(), description: e.description.trim() }),
        onSuccess: () => {
            toast.success('Enregistré.');
            setEdition(null);
            void queryClient.invalidateQueries({ queryKey: [type] });
            void queryClient.invalidateQueries({ queryKey: ['catalogue-gestion'] });
        },
        onError: e => toast.error(e.message),
    });

    if (isPending) return <Chargement />;
    if (error) return <EtatErreur erreur={error} />;

    return (
        <div className="grid gap-4">
            <div>
                <Button variant="outline" onClick={() => setEdition({ id: null, nom: '', description: '', actif: true })}><Plus /> Nouvelle {libelle}</Button>
            </div>
            {data.length === 0 ? <EtatVide message={`Aucune ${libelle}.`} /> : (
                <Card>
                    <CardContent className="divide-y p-0">
                        {data.map(e => (
                            <div key={e.id} className="flex items-center gap-3 px-4 py-2">
                                <span className="flex-1 font-medium">{e.nom}</span>
                                {!e.actif && <Badge variant="outline">Retirée</Badge>}
                                <Button variant="ghost" size="icon" aria-label={`Modifier ${e.nom}`}
                                    onClick={() => setEdition({ id: e.id, nom: e.nom, description: e.description, actif: e.actif })}><Pencil /></Button>
                            </div>
                        ))}
                    </CardContent>
                </Card>
            )}

            <Dialog open={!!edition} onOpenChange={o => { if (!o) setEdition(null); }}>
                <DialogContent>
                    <DialogHeader><DialogTitle>{edition?.id ? `Modifier la ${libelle}` : `Nouvelle ${libelle}`}</DialogTitle></DialogHeader>
                    {edition && (
                        <form id="referentiel" className="grid gap-4" onSubmit={e => { e.preventDefault(); enregistrer.mutate(edition); }}>
                            <div className="grid gap-2">
                                <Label htmlFor="nom-ref">Nom</Label>
                                <Input id="nom-ref" required maxLength={150} value={edition.nom} onChange={e => setEdition({ ...edition, nom: e.target.value })} />
                            </div>
                            <div className="grid gap-2">
                                <Label htmlFor="desc-ref">Description</Label>
                                <Textarea id="desc-ref" maxLength={1000} value={edition.description} onChange={e => setEdition({ ...edition, description: e.target.value })} />
                            </div>
                            {edition.id && (
                                <label className="flex items-center gap-2 text-sm">
                                    <input type="checkbox" className="size-4" checked={edition.actif} onChange={e => setEdition({ ...edition, actif: e.target.checked })} />
                                    Visible dans la boutique (décocher masque tous ses produits)
                                </label>
                            )}
                        </form>
                    )}
                    <DialogFooter>
                        <Button variant="outline" onClick={() => setEdition(null)}>Annuler</Button>
                        <Button type="submit" form="referentiel" disabled={enregistrer.isPending || !edition?.nom.trim()}>Enregistrer</Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </div>
    );
}
