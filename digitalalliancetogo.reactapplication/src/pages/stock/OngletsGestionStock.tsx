import { useState, type FormEvent } from 'react';
import { Link } from 'react-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Loader2, PackagePlus, Plus } from 'lucide-react';
import { toast } from 'sonner';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Chargement, EtatErreur, EtatVide, StatutBadge } from '@/components/commun';
import { DialogueAction } from '@/components/DialogueAction';
import { SelecteurProduit } from '@/components/Selecteurs';
import { api } from '@/lib/api';
import { useAuth } from '@/lib/auth';
import { formatDate } from '@/lib/format';
import type { EcartReception, Entrepot, Produit, ResultatEntreeStock } from '@/lib/types';

function useRafraichirStock() {
    const queryClient = useQueryClient();
    return () => {
        for (const cle of ['stocks', 'ecarts', 'entrepots', 'mouvements', 'a-traiter', 'commandes']) void queryClient.invalidateQueries({ queryKey: [cle] });
    };
}

/** Réception d'une livraison fournisseur ; la quantité commandée permet de détecter un surplus (§30). */
export function Reception() {
    const rafraichir = useRafraichirStock();
    const { data: entrepots } = useQuery({ queryKey: ['entrepots'], queryFn: () => api.get<Entrepot[]>('/stocks/entrepots') });
    const [produit, setProduit] = useState<Produit | null>(null);
    const [entrepotId, setEntrepotId] = useState('');
    const [recue, setRecue] = useState('');
    const [commandee, setCommandee] = useState('');
    const [reference, setReference] = useState('');
    const [dernier, setDernier] = useState<(ResultatEntreeStock & { produit: string }) | null>(null);

    const actifs = entrepots?.filter(e => e.actif) ?? [];
    const entrepot = entrepotId || (actifs.length === 1 ? actifs[0].id : '');

    const recevoir = useMutation({
        mutationFn: () => api.post<ResultatEntreeStock>('/stocks/entrees', {
            produitId: produit!.id,
            entrepotId: entrepot,
            quantite: Number(recue),
            quantiteCommandee: commandee ? Number(commandee) : null,
            reference: reference.trim(),
        }),
        onSuccess: r => {
            setDernier({ ...r, produit: produit!.nom });
            toast.success(`Réception enregistrée : ${Number(recue) - r.surplusEnAttente} unité(s) en stock.`);
            setRecue(''); setCommandee(''); setReference(''); setProduit(null);
            rafraichir();
        },
        onError: e => toast.error(e.message),
    });

    const soumettre = (e: FormEvent) => { e.preventDefault(); recevoir.mutate(); };
    const surplus = commandee && recue ? Number(recue) - Number(commandee) : 0;

    return (
        <div className="grid gap-6 lg:grid-cols-2">
            <Card>
                <CardHeader>
                    <CardTitle className="text-base">Réception fournisseur</CardTitle>
                    <CardDescription>Les commandes payées qui attendaient ce produit sont servies automatiquement, dans l'ordre de paiement.</CardDescription>
                </CardHeader>
                <CardContent>
                    <form onSubmit={soumettre} className="grid gap-4">
                        <div className="grid gap-2">
                            <Label>Produit</Label>
                            <SelecteurProduit libelleSelection={produit?.nom ?? null} surChoix={setProduit} />
                        </div>
                        <div className="grid gap-2">
                            <Label>Entrepôt</Label>
                            <Select value={entrepot} onValueChange={setEntrepotId}>
                                <SelectTrigger><SelectValue placeholder="Choisir un entrepôt" /></SelectTrigger>
                                <SelectContent>{actifs.map(e => <SelectItem key={e.id} value={e.id}>{e.nom}</SelectItem>)}</SelectContent>
                            </Select>
                        </div>
                        <div className="grid grid-cols-2 gap-3">
                            <div className="grid gap-2">
                                <Label htmlFor="recue">Quantité reçue *</Label>
                                <Input id="recue" type="number" min={1} required value={recue} onChange={e => setRecue(e.target.value)} />
                            </div>
                            <div className="grid gap-2">
                                <Label htmlFor="commandee">Quantité commandée</Label>
                                <Input id="commandee" type="number" min={1} value={commandee} onChange={e => setCommandee(e.target.value)} />
                            </div>
                        </div>
                        {surplus > 0 && (
                            <p className="text-sm text-amber-700 dark:text-amber-400">
                                {surplus} unité(s) en surplus : seules les {commandee} commandées entreront en stock, le reste attendra la décision de l'Administrateur.
                            </p>
                        )}
                        <div className="grid gap-2">
                            <Label htmlFor="reference">Bon de livraison / facture *</Label>
                            <Input id="reference" required maxLength={100} value={reference} onChange={e => setReference(e.target.value)} />
                        </div>
                        <Button type="submit" disabled={!produit || !entrepot || !recue || !reference.trim() || recevoir.isPending}>
                            {recevoir.isPending ? <Loader2 className="animate-spin" /> : <PackagePlus />} Enregistrer la réception
                        </Button>
                    </form>
                </CardContent>
            </Card>

            {dernier && (
                <Alert className="h-fit">
                    <PackagePlus />
                    <AlertTitle>{dernier.produit} : {dernier.quantiteDisponible} disponible(s) ({dernier.quantitePhysique} en stock)</AlertTitle>
                    <AlertDescription className="space-y-1">
                        {dernier.commandesReservees.length > 0
                            ? <p>Commandes servies : {dernier.commandesReservees.join(', ')}.</p>
                            : <p>Aucune commande n'attendait ce produit.</p>}
                        {dernier.surplusEnAttente > 0 && <p>{dernier.surplusEnAttente} unité(s) de surplus en attente de décision (onglet Surplus).</p>}
                    </AlertDescription>
                </Alert>
            )}
        </div>
    );
}

/** Surplus fournisseur (§30) : l'Administrateur intègre ou retourne. */
export function Ecarts() {
    const { aRole } = useAuth();
    const rafraichir = useRafraichirStock();
    const [statut, setStatut] = useState('EnAttenteDecision');
    const { data, isPending, error } = useQuery({
        queryKey: ['ecarts', statut],
        queryFn: () => api.get<EcartReception[]>('/stocks/ecarts', { statut }),
    });

    const decider = (id: string, decision: 'IntegrerAuStock' | 'RetournerAuFournisseur', motif: string) =>
        api.post<string[]>(`/stocks/ecarts/${id}/decision`, { decision, motif })
            .then(servies => {
                toast.success(decision === 'IntegrerAuStock'
                    ? `Surplus intégré au stock.${servies.length ? ` Commandes servies : ${servies.join(', ')}.` : ''}`
                    : 'Surplus retourné au fournisseur : rien n\'entre en stock.');
                rafraichir();
            })
            .catch((e: Error) => { toast.error(e.message); throw e; });

    return (
        <div className="space-y-4">
            <Tabs value={statut} onValueChange={setStatut}>
                <TabsList>
                    <TabsTrigger value="EnAttenteDecision">À décider</TabsTrigger>
                    <TabsTrigger value="IntegreAuStock">Intégrés</TabsTrigger>
                    <TabsTrigger value="RetourneFournisseur">Retournés</TabsTrigger>
                </TabsList>
            </Tabs>
            <Card>
                <CardContent className="p-0">
                    {isPending ? <div className="p-4"><Chargement /></div> : error ? <div className="p-4"><EtatErreur erreur={error} /></div>
                        : data.length === 0 ? <EtatVide message="Aucun surplus." /> : (
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    <TableHead>Réception</TableHead>
                                    <TableHead>Produit</TableHead>
                                    <TableHead className="text-right">Commandé</TableHead>
                                    <TableHead className="text-right">Reçu</TableHead>
                                    <TableHead className="text-right">Surplus</TableHead>
                                    <TableHead>Statut</TableHead>
                                    {statut === 'EnAttenteDecision' && aRole() && <TableHead className="text-right">Décision</TableHead>}
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                {data.map(e => (
                                    <TableRow key={e.id}>
                                        <TableCell><div className="font-medium">{e.reference}</div><div className="text-muted-foreground text-xs">{formatDate(e.dateConstat)} · {e.entrepotNom}</div></TableCell>
                                        <TableCell>{e.produitNom}</TableCell>
                                        <TableCell className="text-right tabular-nums">{e.quantiteCommandee}</TableCell>
                                        <TableCell className="text-right tabular-nums">{e.quantiteRecue}</TableCell>
                                        <TableCell className="text-right font-semibold tabular-nums">+{e.surplus}</TableCell>
                                        <TableCell>
                                            <StatutBadge statut={e.statut} />
                                            {e.motifDecision && <div className="text-muted-foreground mt-1 text-xs">{e.motifDecision}</div>}
                                        </TableCell>
                                        {statut === 'EnAttenteDecision' && aRole() && (
                                            <TableCell>
                                                <div className="flex justify-end gap-2">
                                                    <DialogueAction declencheur={<Button size="sm" variant="outline">Retourner</Button>}
                                                        titre={`Retourner ${e.surplus} unité(s) au fournisseur`} champMotif="Motif (accord du fournisseur...)" motifObligatoire
                                                        libelleConfirmer="Retourner" onConfirmer={motif => decider(e.id, 'RetournerAuFournisseur', motif)} />
                                                    <DialogueAction declencheur={<Button size="sm">Intégrer</Button>}
                                                        titre={`Intégrer ${e.surplus} unité(s) au stock`} description="Les commandes en attente de ce produit seront servies."
                                                        champMotif="Motif (facture complémentaire, geste commercial...)" motifObligatoire
                                                        libelleConfirmer="Intégrer" onConfirmer={motif => decider(e.id, 'IntegrerAuStock', motif)} />
                                                </div>
                                            </TableCell>
                                        )}
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                    )}
                </CardContent>
            </Card>
            {statut === 'EnAttenteDecision' && !aRole() && (
                <p className="text-muted-foreground text-xs">Seul l'Administrateur décide : un surplus n'entre jamais en stock sans validation.</p>
            )}
        </div>
    );
}

export function Entrepots({ gestion }: { gestion: boolean }) {
    const rafraichir = useRafraichirStock();
    const [nom, setNom] = useState('');
    const [adresse, setAdresse] = useState('');
    const { data, isPending, error } = useQuery({ queryKey: ['entrepots'], queryFn: () => api.get<Entrepot[]>('/stocks/entrepots') });

    return (
        <Card>
            <CardHeader className="flex flex-row items-center justify-between">
                <CardTitle className="text-base">Entrepôts</CardTitle>
                {gestion && (
                    <DialogueAction declencheur={<Button size="sm"><Plus /> Nouvel entrepôt</Button>} titre="Nouvel entrepôt"
                        libelleConfirmer="Créer"
                        onConfirmer={() => api.post('/stocks/entrepots', { nom: nom.trim(), adresse: adresse.trim() })
                            .then(() => { toast.success('Entrepôt créé.'); setNom(''); setAdresse(''); rafraichir(); })
                            .catch((e: Error) => { toast.error(e.message); throw e; })}>
                        <div className="grid gap-3">
                            <div className="grid gap-2"><Label htmlFor="nomE">Nom</Label><Input id="nomE" value={nom} onChange={e => setNom(e.target.value)} /></div>
                            <div className="grid gap-2"><Label htmlFor="adrE">Adresse</Label><Input id="adrE" value={adresse} onChange={e => setAdresse(e.target.value)} /></div>
                        </div>
                    </DialogueAction>
                )}
            </CardHeader>
            <CardContent className="p-0">
                {isPending ? <div className="p-4"><Chargement /></div> : error ? <div className="p-4"><EtatErreur erreur={error} /></div>
                    : data.length === 0 ? <EtatVide message="Aucun entrepôt : créez-en un pour recevoir de la marchandise." /> : (
                    <Table>
                        <TableHeader><TableRow><TableHead>Nom</TableHead><TableHead>Adresse</TableHead><TableHead>État</TableHead><TableHead /></TableRow></TableHeader>
                        <TableBody>
                            {data.map(e => (
                                <TableRow key={e.id}>
                                    <TableCell className="font-medium">{e.nom}</TableCell>
                                    <TableCell>{e.adresse}</TableCell>
                                    <TableCell>{e.actif ? 'Actif' : 'Désactivé'}</TableCell>
                                    <TableCell className="text-right"><Link className="text-primary text-sm hover:underline" to={`/stock?entrepot=${e.id}`}>Voir le stock</Link></TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                )}
            </CardContent>
        </Card>
    );
}
