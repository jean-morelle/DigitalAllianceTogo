import { useState } from 'react';
import { Link } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { ArrowRight, CheckCircle2 } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Chargement, EnTetePage, EtatErreur } from '@/components/commun';
import { api } from '@/lib/api';
import { useAuth } from '@/lib/auth';
import { Roles } from '@/lib/roles';
import { depuis, formatFcfa } from '@/lib/format';
import type { FileDeTravail, Statistiques } from '@/lib/types';
import { cn } from '@/lib/utils';

/** Écran à ouvrir pour traiter chaque file de travail de l'API. */
const LIENS: Record<string, string> = {
    'paiements-a-verifier': '/paiements',
    'devis-a-envoyer': '/devis?statut=Brouillon',
    'devis-modification-demandee': '/devis?statut=ModificationDemandee',
    'devis-remise-exceptionnelle': '/devis?statut=ValidationInterne',
    'modifications-en-attente-client': '/commandes',
    'modifications-a-valider': '/commandes',
    'sav-decision-commerciale': '/sav?statut=DecisionCommerciale',
    'remboursements-a-valider': '/finance',
    'remboursements-a-executer': '/finance',
    'remboursements-echoues': '/finance',
    'avoirs-a-valider': '/finance?onglet=avoirs',
    'surplus-fournisseur': '/stock?onglet=ecarts',
    'commandes-a-preparer': '/commandes?statut=StockReserve',
    'commandes-a-livrer': '/commandes?statut=PretePourLivraison',
    'retours-a-controler': '/commandes?statut=AnnulationEnCours',
    'commandes-en-rupture': '/commandes?statut=EnAttenteDisponibilite',
    'remplacements-sav-a-livrer': '/sav?statut=RemplacementEnCours',
    'stocks-sous-seuil': '/stock?sousSeuil=true',
    'sav-a-diagnostiquer': '/sav?statut=Ouvert',
    'sav-en-reparation': '/sav?statut=EnReparation',
};

function FilesATraiter() {
    const { data, isPending, error } = useQuery({
        queryKey: ['a-traiter'],
        queryFn: () => api.get<FileDeTravail[]>('/tableau-de-bord/a-traiter'),
        refetchInterval: 60_000,
    });

    if (isPending) return <Chargement lignes={3} />;
    if (error) return <EtatErreur erreur={error} />;

    const enAttente = data.filter(f => f.nombre > 0).sort((a, b) => (a.plusAncien ?? '').localeCompare(b.plusAncien ?? ''));
    if (enAttente.length === 0) {
        return (
            <Card>
                <CardContent className="flex items-center gap-3 py-6">
                    <CheckCircle2 className="size-6 text-emerald-600" />
                    <span>Rien à traiter pour le moment.</span>
                </CardContent>
            </Card>
        );
    }

    return (
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {enAttente.map(f => (
                <Link key={f.cle} to={LIENS[f.cle] ?? '/'} className="group">
                    <Card className="group-hover:border-primary h-full transition-colors">
                        <CardHeader className="pb-2">
                            <CardDescription className="flex items-center justify-between">
                                <span>{f.responsable}</span>
                                <ArrowRight className="size-4 opacity-0 transition-opacity group-hover:opacity-100" />
                            </CardDescription>
                            <CardTitle className="flex items-baseline gap-2">
                                <span className="text-3xl">{f.nombre}</span>
                                <span className="text-sm font-normal">{f.libelle}</span>
                            </CardTitle>
                        </CardHeader>
                        {f.plusAncien && (
                            <CardContent className="text-muted-foreground text-xs">Le plus ancien : {depuis(f.plusAncien)}</CardContent>
                        )}
                    </Card>
                </Link>
            ))}
        </div>
    );
}

function Indicateur({ titre, valeur, detail, className }: { titre: string; valeur: string; detail?: string; className?: string }) {
    return (
        <Card className={className}>
            <CardHeader className="pb-2">
                <CardDescription>{titre}</CardDescription>
                <CardTitle className="text-2xl tabular-nums">{valeur}</CardTitle>
            </CardHeader>
            {detail && <CardContent className="text-muted-foreground text-xs">{detail}</CardContent>}
        </Card>
    );
}

const PERIODES = { '7': '7 jours', '30': '30 jours', '90': '90 jours', '365': '12 mois' } as const;

function StatistiquesPeriode() {
    const [jours, setJours] = useState<keyof typeof PERIODES>('30');

    const { data: s, isPending, error } = useQuery({
        queryKey: ['statistiques', jours],
        // Date calculée à l'exécution de la requête (pas pendant le rendu)
        queryFn: () => api.get<Statistiques>('/tableau-de-bord/statistiques', {
            debut: new Date(Date.now() - Number(jours) * 86_400_000).toISOString(),
        }),
    });

    return (
        <section className="mt-8">
            <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
                <h2 className="text-lg font-semibold">Activité</h2>
                <Tabs value={jours} onValueChange={v => setJours(v as keyof typeof PERIODES)}>
                    <TabsList>
                        {Object.entries(PERIODES).map(([v, l]) => <TabsTrigger key={v} value={v}>{l}</TabsTrigger>)}
                    </TabsList>
                </Tabs>
            </div>

            {isPending ? <Chargement lignes={4} /> : error ? <EtatErreur erreur={error} /> : (
                <div className="space-y-6">
                    <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
                        <Indicateur titre="Encaissé net" valeur={formatFcfa(s.ventes.encaisseNet)}
                            detail={`${formatFcfa(s.ventes.encaisse)} encaissés − ${formatFcfa(s.ventes.rembourse)} remboursés`} />
                        <Indicateur titre="Panier moyen" valeur={formatFcfa(s.ventes.panierMoyen)} detail={`${s.ventes.paiementsConfirmes} paiement(s) confirmé(s)`} />
                        <Indicateur titre="Commandes livrées" valeur={String(s.ventes.commandesLivrees)}
                            detail={`${s.ventes.commandesCreees} créées · ${s.ventes.commandesAnnulees} annulées`} />
                        <Indicateur titre="Devis acceptés" valeur={`${s.devis.tauxAcceptationPourcent} %`}
                            detail={`${s.devis.acceptes} / ${s.devis.crees} · remise moyenne ${s.devis.remiseMoyennePourcent} %`} />
                    </div>

                    <div className="grid gap-4 lg:grid-cols-2">
                        <Card>
                            <CardHeader>
                                <CardTitle className="text-base">Clients par source</CardTitle>
                                <CardDescription>Les réseaux sociaux amènent-ils des clients qui paient ?</CardDescription>
                            </CardHeader>
                            <CardContent>
                                <Table>
                                    <TableHeader>
                                        <TableRow>
                                            <TableHead>Source</TableHead>
                                            <TableHead className="text-right">Nouveaux</TableHead>
                                            <TableHead className="text-right">Ont payé</TableHead>
                                            <TableHead className="text-right">Encaissé</TableHead>
                                        </TableRow>
                                    </TableHeader>
                                    <TableBody>
                                        {s.acquisitionParSource.length === 0 && (
                                            <TableRow><TableCell colSpan={4} className="text-muted-foreground text-center">Aucune donnée</TableCell></TableRow>
                                        )}
                                        {s.acquisitionParSource.map(a => (
                                            <TableRow key={a.source}>
                                                <TableCell className="font-medium">{a.source}</TableCell>
                                                <TableCell className="text-right tabular-nums">{a.nouveauxClients}</TableCell>
                                                <TableCell className="text-right tabular-nums">{a.clientsAyantPaye} <span className="text-muted-foreground">({a.tauxConversionPourcent} %)</span></TableCell>
                                                <TableCell className="text-right tabular-nums">{formatFcfa(a.encaisse)}</TableCell>
                                            </TableRow>
                                        ))}
                                    </TableBody>
                                </Table>
                            </CardContent>
                        </Card>

                        <Card>
                            <CardHeader>
                                <CardTitle className="text-base">Produits les plus livrés</CardTitle>
                            </CardHeader>
                            <CardContent>
                                <Table>
                                    <TableHeader>
                                        <TableRow>
                                            <TableHead>Produit</TableHead>
                                            <TableHead className="text-right">Qté</TableHead>
                                            <TableHead className="text-right">Montant</TableHead>
                                        </TableRow>
                                    </TableHeader>
                                    <TableBody>
                                        {s.topProduitsLivres.length === 0 && (
                                            <TableRow><TableCell colSpan={3} className="text-muted-foreground text-center">Aucune livraison sur la période</TableCell></TableRow>
                                        )}
                                        {s.topProduitsLivres.map(p => (
                                            <TableRow key={p.produitId}>
                                                <TableCell><div className="font-medium">{p.nom}</div><div className="text-muted-foreground text-xs">{p.reference}</div></TableCell>
                                                <TableCell className="text-right tabular-nums">{p.quantite}</TableCell>
                                                <TableCell className="text-right tabular-nums">{formatFcfa(p.montant)}</TableCell>
                                            </TableRow>
                                        ))}
                                    </TableBody>
                                </Table>
                            </CardContent>
                        </Card>
                    </div>

                    <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
                        <Indicateur titre="Livraisons réussies" valeur={`${s.livraisons.tauxReussitePourcent} %`}
                            detail={`${s.livraisons.remises} remises au livreur · ${s.livraisons.aReprogrammer} à reprogrammer · ${s.livraisons.refusClient} refus`} />
                        <Indicateur titre="Délai paiement → livraison"
                            valeur={s.livraisons.delaiMoyenPaiementLivraisonHeures === null ? '—' : `${s.livraisons.delaiMoyenPaiementLivraisonHeures} h`} />
                        <Indicateur titre="SAV clôturés" valeur={String(s.sav.clotures)}
                            detail={`${s.sav.repares} réparés · ${s.sav.remplaces} remplacés · ${s.sav.rembourses + s.sav.avoirs} remboursés/avoir`} />
                        <Indicateur titre="À régulariser" valeur={formatFcfa(s.instantane.remboursementsNonRegles)}
                            className={cn(s.instantane.remboursementsNonRegles > 0 && 'border-amber-300')}
                            detail={`Avoirs disponibles : ${formatFcfa(s.instantane.avoirsDisponibles)}`} />
                    </div>
                </div>
            )}
        </section>
    );
}

export function TableauDeBordPage() {
    const { session, aRole } = useAuth();
    return (
        <>
            <EnTetePage titre={`Bonjour ${session?.prenom ?? ''}`} description="Ce qui attend une action de votre part." />
            <FilesATraiter />
            {aRole(Roles.Commercial) && <StatistiquesPeriode />}
        </>
    );
}
