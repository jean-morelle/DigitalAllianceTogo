import { useState } from 'react';
import { Link, useSearchParams } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { ImageOff, Search, ShoppingCart } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Chargement, EtatErreur, EtatVide } from '@/components/commun';
import { Pagination } from '@/components/Pagination';
import { api } from '@/lib/api';
import { formatFcfa } from '@/lib/format';
import type { Categorie, PaginatedList, ProduitCatalogue } from '@/lib/types';
import { useQuantitePanier } from './panier';

const TOUTES = 'toutes';

export function ImageProduit({ url, nom, className }: { url: string | null; nom: string; className?: string }) {
    return url
        ? <img src={url} alt={nom} loading="lazy" className={className ?? 'aspect-square w-full rounded-md bg-white object-contain'} />
        : <div className={className ?? 'bg-muted text-muted-foreground flex aspect-square w-full items-center justify-center rounded-md'}><ImageOff className="size-10 opacity-40" /></div>;
}

export function BadgeStock({ enStock }: { enStock: boolean }) {
    return enStock
        ? <Badge variant="secondary" className="bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300">En stock</Badge>
        : <Badge variant="secondary">Sur commande</Badge>;
}

/** Catalogue public : accessible sans compte (liens partagés sur WhatsApp, TikTok, Facebook — §37). */
export function BoutiquePage() {
    const [params, setParams] = useSearchParams();
    const [recherche, setRecherche] = useState(params.get('q') ?? '');
    const categorieId = params.get('categorie') ?? TOUTES;
    const page = Number(params.get('page') ?? 1);
    const panier = useQuantitePanier();

    const { data: categories } = useQuery({ queryKey: ['categories'], queryFn: () => api.get<Categorie[]>('/categories'), select: liste => liste.filter(c => c.actif !== false) });
    const { data, isPending, error } = useQuery({
        queryKey: ['catalogue', params.get('q'), categorieId, page],
        queryFn: () => api.get<PaginatedList<ProduitCatalogue>>('/produits', {
            actif: true,
            recherche: params.get('q'),
            categorieId: categorieId === TOUTES ? undefined : categorieId,
            pageNumber: page,
            pageSize: 24,
        }),
    });

    const maj = (changements: Record<string, string | null>) => {
        const suivants = new URLSearchParams(params);
        for (const [cle, valeur] of Object.entries(changements)) {
            if (!valeur || valeur === TOUTES) suivants.delete(cle); else suivants.set(cle, valeur);
        }
        if (!('page' in changements)) suivants.delete('page');
        setParams(suivants);
    };

    return (
        <>
            <div className="mb-6">
                <h1 className="text-2xl font-semibold tracking-tight">Matériel informatique</h1>
                <p className="text-muted-foreground mt-1 text-sm">Ordinateurs, accessoires et réseau. Paiement Mobile Money, livraison à Lomé et environs.</p>
            </div>

            <div className="mb-6 flex flex-col gap-2 sm:flex-row">
                <form className="relative flex-1" onSubmit={e => { e.preventDefault(); maj({ q: recherche.trim() }); }}>
                    <Search className="text-muted-foreground absolute top-1/2 left-3 size-4 -translate-y-1/2" />
                    <Input className="pl-9" placeholder="Rechercher un produit (ex : HP, souris, 16 Go...)" value={recherche} onChange={e => setRecherche(e.target.value)} />
                </form>
                <Select value={categorieId} onValueChange={v => maj({ categorie: v })}>
                    <SelectTrigger className="sm:w-60"><SelectValue /></SelectTrigger>
                    <SelectContent>
                        <SelectItem value={TOUTES}>Toutes les catégories</SelectItem>
                        {categories?.map(c => <SelectItem key={c.id} value={c.id}>{c.nom}</SelectItem>)}
                    </SelectContent>
                </Select>
            </div>

            {isPending ? <Chargement lignes={6} /> : error ? <EtatErreur erreur={error} /> : data.items.length === 0 ? <EtatVide message="Aucun produit ne correspond à votre recherche." /> : (
                <div className="grid grid-cols-2 gap-3 sm:gap-4 md:grid-cols-3 lg:grid-cols-4">
                    {data.items.map(p => (
                        <Card key={p.id} className="flex flex-col overflow-hidden">
                            <Link to={`/boutique/produits/${p.id}`} className="block p-3">
                                <ImageProduit url={p.imagePrincipaleUrl} nom={p.nom} />
                            </Link>
                            <CardContent className="flex flex-1 flex-col gap-2 px-3 pb-3">
                                <div className="text-muted-foreground text-xs">{p.marqueNom} · {p.categorieNom}</div>
                                <Link to={`/boutique/produits/${p.id}`} className="line-clamp-2 text-sm font-medium hover:underline">{p.nom}</Link>
                                <div className="mt-auto flex items-center justify-between gap-2">
                                    <span className="font-semibold tabular-nums">{formatFcfa(p.prix)}</span>
                                    <BadgeStock enStock={p.enStock} />
                                </div>
                                <Button size="sm" disabled={panier.enCours} onClick={() => panier.ajouter(p.id, 1, `${p.nom} ajouté au panier.`)}>
                                    <ShoppingCart /> Ajouter
                                </Button>
                            </CardContent>
                        </Card>
                    ))}
                </div>
            )}
            {data && <Pagination page={page} totalPages={data.totalPages} surChangement={p => maj({ page: String(p) })} />}
        </>
    );
}
