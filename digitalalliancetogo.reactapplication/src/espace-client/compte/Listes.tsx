import { Link } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { ChevronRight } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Chargement, EtatErreur, EtatVide, StatutBadge } from '@/components/commun';
import { api } from '@/lib/api';
import { formatDate, formatFcfa } from '@/lib/format';
import type { Avoir, Commande, Devis, PaginatedList, TicketSav } from '@/lib/types';
import { STATUT_CLIENT } from '../client';

function Ligne({ vers, titre, sousTitre, droite, statut }: { vers?: string; titre: string; sousTitre: string; droite: string; statut: string }) {
    const contenu = (
        <CardContent className="flex items-center gap-3 p-4">
            <div className="min-w-0 flex-1">
                <div className="font-medium">{titre}</div>
                <div className="text-muted-foreground text-xs">{sousTitre}</div>
            </div>
            <div className="text-right">
                <div className="font-semibold tabular-nums">{droite}</div>
                <StatutBadge statut={statut} className="mt-1" />
            </div>
            {vers && <ChevronRight className="text-muted-foreground size-4" />}
        </CardContent>
    );
    return <Card>{vers ? <Link to={vers} className="block hover:opacity-90">{contenu}</Link> : contenu}</Card>;
}

export function MesCommandes() {
    const { data, isPending, error } = useQuery({
        queryKey: ['mes-commandes'],
        queryFn: () => api.get<PaginatedList<Commande>>('/commandes', { pageSize: 50 }),
    });
    if (isPending) return <Chargement />;
    if (error) return <EtatErreur erreur={error} />;
    if (data.items.length === 0) return <EtatVide message="Aucune commande pour le moment." />;
    return (
        <div className="grid gap-3">
            {data.items.map(c => (
                <Card key={c.id}>
                    <Link to={`/compte/commandes/${c.id}`} className="block hover:opacity-90">
                        <CardContent className="flex items-center gap-3 p-4">
                            <div className="min-w-0 flex-1">
                                <div className="font-medium">{c.reference}</div>
                                <div className="text-muted-foreground text-xs">{formatDate(c.dateCreation, false)}</div>
                            </div>
                            <div className="text-right">
                                <div className="font-semibold tabular-nums">{formatFcfa(c.total)}</div>
                                <div className="text-sm">{STATUT_CLIENT[c.statut] ?? c.statut}</div>
                            </div>
                            <ChevronRight className="text-muted-foreground size-4" />
                        </CardContent>
                    </Link>
                </Card>
            ))}
        </div>
    );
}

export function MesDevis() {
    const { data, isPending, error } = useQuery({
        queryKey: ['mes-devis'],
        queryFn: () => api.get<PaginatedList<Devis>>('/devis', { pageSize: 50 }),
    });
    if (isPending) return <Chargement />;
    if (error) return <EtatErreur erreur={error} />;
    if (data.items.length === 0) return <EtatVide message="Aucun devis. Depuis le panier, « Demander un devis » pour un prix négocié." />;
    return (
        <div className="grid gap-3">
            {data.items.map(d => (
                <Ligne key={d.id} vers={`/compte/devis/${d.id}`} titre={d.reference}
                    sousTitre={d.statut === 'Envoye' ? `Valable jusqu'au ${formatDate(d.dateValidite, false)}` : formatDate(d.dateCreation, false)}
                    droite={formatFcfa(d.total)} statut={d.statut === 'Brouillon' || d.statut === 'ValidationInterne' ? 'EnAttente' : d.statut} />
            ))}
        </div>
    );
}

export function MesTicketsSav() {
    const { data, isPending, error } = useQuery({
        queryKey: ['mes-tickets'],
        queryFn: () => api.get<PaginatedList<TicketSav>>('/sav', { pageSize: 50 }),
    });
    if (isPending) return <Chargement />;
    if (error) return <EtatErreur erreur={error} />;
    return (
        <div className="grid gap-3">
            <p className="text-muted-foreground text-sm">Un problème avec un produit livré ? Ouvrez un ticket depuis la commande concernée.</p>
            {data.items.length === 0 ? <EtatVide message="Aucune demande SAV." /> : data.items.map(t => (
                <Card key={t.id}>
                    <CardContent className="grid gap-1 p-4">
                        <div className="flex items-center justify-between gap-2">
                            <span className="font-medium">{t.reference} · {t.produitNom}</span>
                            <StatutBadge statut={t.statut} />
                        </div>
                        <div className="text-muted-foreground text-sm">{t.motif}</div>
                        {t.resolution && <div className="text-sm">Résultat : {t.resolution}</div>}
                        <div className="text-muted-foreground text-xs">Commande {t.commandeReference} · ouvert le {formatDate(t.dateCreation, false)}</div>
                    </CardContent>
                </Card>
            ))}
        </div>
    );
}

export function MesAvoirs() {
    const { data, isPending, error } = useQuery({
        queryKey: ['mes-avoirs'],
        queryFn: () => api.get<PaginatedList<Avoir>>('/avoirs', { pageSize: 50 }),
    });
    if (isPending) return <Chargement />;
    if (error) return <EtatErreur erreur={error} />;
    return (
        <div className="grid gap-3">
            <p className="text-muted-foreground text-sm">Un avoir disponible se déduit du paiement d'une prochaine commande.</p>
            {data.items.length === 0 ? <EtatVide message="Aucun avoir." /> : data.items.map(a => (
                <Ligne key={a.id} titre={a.reference} sousTitre={a.motif}
                    droite={a.statut === 'Disponible' ? `${formatFcfa(a.montantRestant)} disponibles` : formatFcfa(a.montant)} statut={a.statut} />
            ))}
        </div>
    );
}
