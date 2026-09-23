import { useState } from 'react';
import { Link, useNavigate } from 'react-router';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { AlertTriangle, FileText, Loader2, ShoppingBag, Trash2 } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Chargement, EtatErreur } from '@/components/commun';
import { api } from '@/lib/api';
import { formatFcfa } from '@/lib/format';
import { ChoixAdresse } from './Adresses';
import { adresseParDefaut, useMaFiche } from './client';
import { ImageProduit } from './BoutiquePage';
import { usePanier, useQuantitePanier } from './panier';

/**
 * Panier (§6) : deux sorties.
 * - Commander : prix du catalogue, paiement Mobile Money ensuite.
 * - Demander un devis : le Commercial peut accorder une remise (gros volumes, entreprises).
 */
export function PanierPage() {
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const { data: panier, isPending, error } = usePanier();
    const { data: fiche } = useMaFiche();
    const quantite = useQuantitePanier();
    const [etape, setEtape] = useState<'panier' | 'commander' | 'devis'>('panier');
    const [adresseChoisie, setAdresseChoisie] = useState('');
    const [telephone, setTelephone] = useState('');
    const [commentaire, setCommentaire] = useState('');
    const adresseId = adresseParDefaut(fiche?.adresses, adresseChoisie);

    const rafraichir = () => { for (const cle of ['panier', 'mes-commandes', 'mes-devis']) void queryClient.invalidateQueries({ queryKey: [cle] }); };

    const commander = useMutation({
        mutationFn: () => api.post<string>('/panier/commander', { adresseLivraisonId: adresseId, telephoneContact: telephone.trim() || null }),
        onSuccess: id => { rafraichir(); toast.success('Commande créée : il reste à la payer.'); navigate(`/compte/commandes/${id}`); },
        onError: e => toast.error(e.message),
    });
    const demanderDevis = useMutation({
        mutationFn: () => api.post<string>('/panier/demande-devis', { commentaire: commentaire.trim() || null }),
        onSuccess: id => { rafraichir(); toast.success('Demande envoyée : un commercial vous répond rapidement.'); navigate(`/compte/devis/${id}`); },
        onError: e => toast.error(e.message),
    });

    if (isPending) return <Chargement />;
    if (error) return <EtatErreur erreur={error} />;

    if (panier.lignes.length === 0) {
        return (
            <div className="flex flex-col items-center gap-4 py-16 text-center">
                <ShoppingBag className="text-muted-foreground size-12" />
                <p>Votre panier est vide.</p>
                <Button asChild><Link to="/boutique">Voir les produits</Link></Button>
            </div>
        );
    }

    return (
        <div className="grid gap-6 lg:grid-cols-[1fr_22rem]">
            <div className="space-y-3">
                <h1 className="text-2xl font-semibold">Mon panier</h1>
                {panier.lignes.map(l => (
                    <Card key={l.produitId}>
                        <CardContent className="flex gap-3 p-3">
                            <Link to={`/boutique/produits/${l.produitId}`} className="size-20 shrink-0">
                                <ImageProduit url={l.imageUrl} nom={l.nom} className="size-20 rounded-md bg-white object-contain" />
                            </Link>
                            <div className="min-w-0 flex-1">
                                <Link to={`/boutique/produits/${l.produitId}`} className="line-clamp-2 font-medium hover:underline">{l.nom}</Link>
                                <div className="text-muted-foreground text-sm">{formatFcfa(l.prixUnitaire)} l'unité</div>
                                {!l.actif && <div className="text-destructive text-xs">Plus proposé à la vente : retirez-le.</div>}
                                {l.actif && l.quantiteDisponible < l.quantite && (
                                    <div className="text-xs text-amber-700 dark:text-amber-400">
                                        {l.quantiteDisponible > 0 ? `${l.quantiteDisponible} en stock : le reste sera livré après réapprovisionnement.` : 'Sur commande : livraison après réapprovisionnement.'}
                                    </div>
                                )}
                                <div className="mt-2 flex items-center gap-2">
                                    <Input type="number" min={1} max={100} className="h-8 w-20" aria-label={`Quantité ${l.nom}`} value={l.quantite}
                                        onChange={e => quantite.definir(l.produitId, Math.min(100, Math.max(1, Math.floor(Number(e.target.value) || 1))))} />
                                    <Button variant="ghost" size="icon" aria-label={`Retirer ${l.nom}`} onClick={() => quantite.definir(l.produitId, 0)}><Trash2 /></Button>
                                    <span className="ml-auto font-semibold tabular-nums">{formatFcfa(l.total)}</span>
                                </div>
                            </div>
                        </CardContent>
                    </Card>
                ))}
            </div>

            <Card className="h-fit lg:sticky lg:top-20">
                <CardHeader>
                    <CardTitle className="flex items-baseline justify-between text-base">
                        <span>Total ({panier.nombreArticles} article{panier.nombreArticles > 1 ? 's' : ''})</span>
                        <span className="text-xl tabular-nums">{formatFcfa(panier.total)}</span>
                    </CardTitle>
                    {!panier.toutDisponible && (
                        <CardDescription className="flex gap-2 text-amber-700 dark:text-amber-400">
                            <AlertTriangle className="size-4 shrink-0" /> Certains produits ne sont pas en stock en quantité suffisante.
                        </CardDescription>
                    )}
                </CardHeader>
                <CardContent className="grid gap-4">
                    {etape === 'panier' && (
                        <>
                            <Button size="lg" onClick={() => setEtape('commander')}>Commander</Button>
                            <Button variant="outline" onClick={() => setEtape('devis')}><FileText /> Demander un devis</Button>
                            <p className="text-muted-foreground text-xs">Grosse quantité ou entreprise ? Demandez un devis : un commercial vous proposera le meilleur prix.</p>
                        </>
                    )}

                    {etape === 'commander' && (
                        <>
                            <div className="grid gap-2">
                                <Label>Adresse de livraison</Label>
                                <ChoixAdresse valeur={adresseId} surChangement={setAdresseChoisie} />
                            </div>
                            <div className="grid gap-2">
                                <Label htmlFor="tel">Téléphone à appeler pour la livraison</Label>
                                <Input id="tel" placeholder={fiche?.telephone} value={telephone} onChange={e => setTelephone(e.target.value)} />
                            </div>
                            <Button size="lg" disabled={!adresseId || commander.isPending} onClick={() => commander.mutate()}>
                                {commander.isPending && <Loader2 className="animate-spin" />} Valider la commande
                            </Button>
                            <p className="text-muted-foreground text-xs">Vous paierez ensuite par Mobile Money (T-Money, Flooz) depuis votre commande. Le stock vous est réservé dès que le paiement est confirmé.</p>
                            <Button variant="ghost" onClick={() => setEtape('panier')}>Retour</Button>
                        </>
                    )}

                    {etape === 'devis' && (
                        <>
                            <div className="grid gap-2">
                                <Label htmlFor="commentaire">Votre demande (facultatif)</Label>
                                <Textarea id="commentaire" placeholder="Ex : facture au nom de l'entreprise, délai souhaité, budget..." value={commentaire}
                                    onChange={e => setCommentaire(e.target.value)} maxLength={1000} />
                            </div>
                            <Button size="lg" disabled={demanderDevis.isPending} onClick={() => demanderDevis.mutate()}>
                                {demanderDevis.isPending && <Loader2 className="animate-spin" />} Envoyer la demande de devis
                            </Button>
                            <Button variant="ghost" onClick={() => setEtape('panier')}>Retour</Button>
                        </>
                    )}
                </CardContent>
            </Card>
        </div>
    );
}
