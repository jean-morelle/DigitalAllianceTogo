import { useState } from 'react';
import { Link, useParams } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { ArrowLeft, MessageCircle, ShoppingCart } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Table, TableBody, TableCell, TableRow } from '@/components/ui/table';
import { Chargement, EtatErreur } from '@/components/commun';
import { api } from '@/lib/api';
import { formatFcfa } from '@/lib/format';
import type { ProduitDetail } from '@/lib/types';
import { BadgeStock, ImageProduit } from './BoutiquePage';
import { useQuantitePanier } from './panier';

/** Fiche produit publique : caractéristiques structurées (§5), prix, disponibilité, partage WhatsApp. */
export function ProduitPage() {
    const { id = '' } = useParams();
    const [quantite, setQuantite] = useState(1);
    const [image, setImage] = useState(0);
    const panier = useQuantitePanier();
    const { data: p, isPending, error } = useQuery({
        queryKey: ['produit', id],
        queryFn: () => api.get<ProduitDetail>(`/produits/${id}`),
    });

    if (isPending) return <Chargement lignes={6} />;
    if (error) return <EtatErreur erreur={error} />;

    const images = [...p.images].sort((a, b) => Number(b.estPrincipale) - Number(a.estPrincipale) || a.ordre - b.ordre);
    const partage = `https://wa.me/?text=${encodeURIComponent(`${p.nom} — ${formatFcfa(p.prix)} : ${window.location.href}`)}`;

    return (
        <>
            <Button asChild variant="ghost" size="sm" className="mb-4 -ml-2">
                <Link to="/boutique"><ArrowLeft /> Boutique</Link>
            </Button>

            <div className="grid gap-8 md:grid-cols-2">
                <div className="space-y-3">
                    <div className="bg-background rounded-lg border p-4">
                        <ImageProduit url={images[image]?.url ?? null} nom={p.nom} />
                    </div>
                    {images.length > 1 && (
                        <div className="flex gap-2 overflow-x-auto">
                            {images.map((img, i) => (
                                <button key={img.id} type="button" onClick={() => setImage(i)}
                                    className={`bg-background size-16 shrink-0 rounded-md border p-1 ${i === image ? 'border-primary' : ''}`}>
                                    <img src={img.url} alt="" className="size-full object-contain" />
                                </button>
                            ))}
                        </div>
                    )}
                </div>

                <div className="space-y-5">
                    <div>
                        <div className="text-muted-foreground text-sm">{p.marqueNom} · {p.categorieNom} · réf. {p.reference}</div>
                        <h1 className="mt-1 text-2xl font-semibold">{p.nom}</h1>
                    </div>
                    <div className="flex items-center gap-3">
                        <span className="text-3xl font-bold tabular-nums">{formatFcfa(p.prix)}</span>
                        <BadgeStock enStock={p.enStock} />
                    </div>

                    {p.actif ? (
                        <div className="flex gap-2">
                            <Input type="number" min={1} max={100} className="w-20" aria-label="Quantité" value={quantite}
                                onChange={e => setQuantite(Math.min(100, Math.max(1, Math.floor(Number(e.target.value) || 1))))} />
                            <Button className="flex-1" disabled={panier.enCours}
                                onClick={() => panier.ajouter(p.id, quantite, `${quantite} × ${p.nom} ajouté(s) au panier.`)}>
                                <ShoppingCart /> Ajouter au panier
                            </Button>
                        </div>
                    ) : (
                        <p className="text-muted-foreground">Ce produit n'est plus proposé à la vente.</p>
                    )}
                    <Button asChild variant="outline" className="w-full">
                        <a href={partage} target="_blank" rel="noreferrer"><MessageCircle /> Partager sur WhatsApp</a>
                    </Button>

                    {p.description && <p className="text-sm leading-relaxed whitespace-pre-line">{p.description}</p>}

                    {p.attributs.length > 0 && (
                        <div>
                            <h2 className="mb-2 font-semibold">Caractéristiques</h2>
                            <Table>
                                <TableBody>
                                    {[...p.attributs].sort((a, b) => a.ordre - b.ordre).map(a => (
                                        <TableRow key={a.id}>
                                            <TableCell className="text-muted-foreground w-1/3">{a.cle}</TableCell>
                                            <TableCell>{a.valeur}</TableCell>
                                        </TableRow>
                                    ))}
                                </TableBody>
                            </Table>
                        </div>
                    )}
                </div>
            </div>
        </>
    );
}
