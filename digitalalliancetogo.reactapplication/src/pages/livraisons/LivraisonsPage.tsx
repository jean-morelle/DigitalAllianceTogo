import { useState } from 'react';
import { Link } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { MapPin, MessageCircle, Phone } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Chargement, EnTetePage, EtatErreur, EtatVide, StatutBadge } from '@/components/commun';
import { Pagination } from '@/components/Pagination';
import { api } from '@/lib/api';
import { useAuth } from '@/lib/auth';
import { formatDate, libelle } from '@/lib/format';
import { Roles } from '@/lib/roles';
import type { Livraison, Livreur, PaginatedList } from '@/lib/types';
import { BoutonRemettre, DialogueEchec, DialogueLivree } from './ActionsLivraison';

const EN_COURS = ['Planifiee', 'EnTransit'];

/** Numéro togolais au format international pour les liens tel: et WhatsApp. */
function numeroInternational(telephone: string): string {
    const chiffres = telephone.replace(/\D/g, '');
    return chiffres.startsWith('228') ? chiffres : `228${chiffres}`;
}

function Destinataire({ l }: { l: Livraison }) {
    const numero = numeroInternational(l.telephoneContact);
    return (
        <div className="space-y-2 text-sm">
            <div className="flex items-start gap-2">
                <MapPin className="text-muted-foreground mt-0.5 size-4 shrink-0" />
                <span>{l.adresseLigne1}{l.adresseLigne2 && `, ${l.adresseLigne2}`}, {l.ville}</span>
            </div>
            <div className="flex flex-wrap gap-2">
                <Button asChild size="sm" variant="outline"><a href={`tel:+${numero}`}><Phone /> {l.telephoneContact}</a></Button>
                <Button asChild size="sm" variant="outline"><a href={`https://wa.me/${numero}`} target="_blank" rel="noreferrer"><MessageCircle /> WhatsApp</a></Button>
            </div>
        </div>
    );
}

function TypeLivraison({ type }: { type: string }) {
    return type === 'Initial' ? null : <Badge variant="outline">{libelle(type)}</Badge>;
}

/** Vue du livreur : ses livraisons en cartes, pensées pour le téléphone. */
function MesLivraisons() {
    const [onglet, setOnglet] = useState<'a-faire' | 'terminees'>('a-faire');
    const { data, isPending, error } = useQuery({
        queryKey: ['livraisons', 'miennes'],
        queryFn: () => api.get<PaginatedList<Livraison>>('/livraisons', { pageSize: 100 }),
        refetchInterval: 60_000,
    });

    if (isPending) return <Chargement />;
    if (error) return <EtatErreur erreur={error} />;
    const liste = data.items.filter(l => (onglet === 'a-faire') === EN_COURS.includes(l.statut));

    return (
        <>
            <Tabs value={onglet} onValueChange={v => setOnglet(v as typeof onglet)} className="mb-4">
                <TabsList>
                    <TabsTrigger value="a-faire">À faire ({data.items.filter(l => EN_COURS.includes(l.statut)).length})</TabsTrigger>
                    <TabsTrigger value="terminees">Terminées</TabsTrigger>
                </TabsList>
            </Tabs>
            {liste.length === 0 ? <EtatVide message={onglet === 'a-faire' ? 'Aucune livraison prévue.' : 'Aucune livraison terminée.'} /> : (
                <div className="grid gap-4 md:grid-cols-2">
                    {liste.map(l => (
                        <Card key={l.id}>
                            <CardHeader className="pb-3">
                                <CardTitle className="flex flex-wrap items-center gap-2 text-base">
                                    {l.commandeReference} <StatutBadge statut={l.statut} /> <TypeLivraison type={l.type} />
                                </CardTitle>
                                <CardDescription>{l.reference} · prévue le {formatDate(l.datePlanifiee, false)}</CardDescription>
                            </CardHeader>
                            <CardContent className="space-y-4">
                                <Destinataire l={l} />
                                {l.statut === 'Planifiee' && <p className="text-muted-foreground text-sm">En attente de la remise du colis au dépôt.</p>}
                                {l.statut === 'EnTransit' && (
                                    <div className="flex gap-2">
                                        <DialogueLivree livraison={l} />
                                        <DialogueEchec livraison={l} />
                                    </div>
                                )}
                                {l.motifEchec && <p className="text-sm text-red-600">{l.motifEchec}</p>}
                                {l.reserve && <p className="text-sm text-amber-700">Réserve : {l.reserve}</p>}
                            </CardContent>
                        </Card>
                    ))}
                </div>
            )}
        </>
    );
}

const TOUS = 'tous';
const STATUTS = ['Planifiee', 'EnTransit', 'Livree', 'LivreeAvecReserve', 'AReprogrammer', 'Echouee', 'Retournee'];

/** Vue du Gestionnaire de stock / Admin : toutes les livraisons, remise au livreur. */
function ToutesLivraisons() {
    const [statut, setStatut] = useState(TOUS);
    const [livreurId, setLivreurId] = useState(TOUS);
    const [page, setPage] = useState(1);
    const { data: livreurs } = useQuery({ queryKey: ['livreurs'], queryFn: () => api.get<Livreur[]>('/livraisons/livreurs') });
    const { data, isPending, error } = useQuery({
        queryKey: ['livraisons', statut, livreurId, page],
        queryFn: () => api.get<PaginatedList<Livraison>>('/livraisons', {
            statut: statut === TOUS ? undefined : statut,
            livreurId: livreurId === TOUS ? undefined : livreurId,
            pageNumber: page,
        }),
    });

    return (
        <>
            <div className="mb-4 flex flex-col gap-2 sm:flex-row">
                <Select value={statut} onValueChange={v => { setStatut(v); setPage(1); }}>
                    <SelectTrigger className="sm:w-56"><SelectValue /></SelectTrigger>
                    <SelectContent>
                        <SelectItem value={TOUS}>Tous les statuts</SelectItem>
                        {STATUTS.map(s => <SelectItem key={s} value={s}>{libelle(s)}</SelectItem>)}
                    </SelectContent>
                </Select>
                <Select value={livreurId} onValueChange={v => { setLivreurId(v); setPage(1); }}>
                    <SelectTrigger className="sm:w-56"><SelectValue /></SelectTrigger>
                    <SelectContent>
                        <SelectItem value={TOUS}>Tous les livreurs</SelectItem>
                        {livreurs?.map(l => <SelectItem key={l.id} value={l.id}>{l.nom} ({l.livraisonsEnCours} en cours)</SelectItem>)}
                    </SelectContent>
                </Select>
            </div>

            <Card>
                <CardContent className="p-0">
                    {isPending ? <div className="p-4"><Chargement /></div> : error ? <div className="p-4"><EtatErreur erreur={error} /></div>
                        : data.items.length === 0 ? <EtatVide message="Aucune livraison. Elles se planifient depuis une commande prête." /> : (
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    <TableHead>Livraison</TableHead>
                                    <TableHead>Commande</TableHead>
                                    <TableHead>Livreur</TableHead>
                                    <TableHead>Destination</TableHead>
                                    <TableHead>Statut</TableHead>
                                    <TableHead className="text-right">Actions</TableHead>
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                {data.items.map(l => (
                                    <TableRow key={l.id}>
                                        <TableCell>
                                            <div className="font-medium">{l.reference}</div>
                                            <div className="text-muted-foreground text-xs">prévue le {formatDate(l.datePlanifiee, false)}</div>
                                        </TableCell>
                                        <TableCell>
                                            <Link to={`/commandes/${l.commandeId}`} className="text-primary hover:underline">{l.commandeReference}</Link>
                                            <div><TypeLivraison type={l.type} /></div>
                                        </TableCell>
                                        <TableCell>{l.livreurNom ?? '—'}</TableCell>
                                        <TableCell className="text-sm">{l.ville}<div className="text-muted-foreground text-xs">{l.telephoneContact}</div></TableCell>
                                        <TableCell>
                                            <StatutBadge statut={l.statut} />
                                            {l.motifEchec && <div className="mt-1 max-w-48 text-xs text-red-600">{l.motifEchec}</div>}
                                            {l.reserve && <div className="mt-1 max-w-48 text-xs text-amber-700">Réserve : {l.reserve}</div>}
                                            {l.preuve && (
                                                <div className="mt-1 text-xs">
                                                    {l.preuve.photoUrl && <a className="text-primary mr-2 hover:underline" href={l.preuve.photoUrl} target="_blank" rel="noreferrer">photo</a>}
                                                    {l.preuve.signatureUrl && <a className="text-primary mr-2 hover:underline" href={l.preuve.signatureUrl} target="_blank" rel="noreferrer">signature</a>}
                                                    {l.preuve.latitude !== null && (
                                                        <a className="text-primary hover:underline" target="_blank" rel="noreferrer"
                                                            href={`https://www.google.com/maps?q=${l.preuve.latitude},${l.preuve.longitude}`}>GPS</a>
                                                    )}
                                                </div>
                                            )}
                                        </TableCell>
                                        <TableCell>
                                            <div className="flex justify-end gap-2">
                                                {l.statut === 'Planifiee' && <BoutonRemettre livraison={l} />}
                                                {l.statut === 'EnTransit' && <><DialogueLivree livraison={l} /><DialogueEchec livraison={l} /></>}
                                            </div>
                                        </TableCell>
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                    )}
                </CardContent>
            </Card>
            {data && <Pagination page={page} totalPages={data.totalPages} surChangement={setPage} />}
        </>
    );
}

export function LivraisonsPage() {
    const { aRole } = useAuth();
    const gestion = aRole(Roles.GestionnaireStock);
    return (
        <>
            <EnTetePage
                titre={gestion ? 'Livraisons' : 'Mes livraisons'}
                description={gestion
                    ? 'La remise du colis au livreur fait sortir le stock ; le livreur confirme ensuite avec une preuve.'
                    : 'Appelez le client avant de partir. À la remise, prenez une photo ou une signature.'}
            />
            {gestion ? <ToutesLivraisons /> : <MesLivraisons />}
        </>
    );
}
