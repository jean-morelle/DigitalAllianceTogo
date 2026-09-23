import { Fragment, useState, type FormEvent } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Bot, ChevronDown, ChevronRight, Lock, Search } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Chargement, EnTetePage, EtatErreur, EtatVide } from '@/components/commun';
import { Pagination } from '@/components/Pagination';
import { api } from '@/lib/api';
import { formatDate } from '@/lib/format';
import type { JournalAudit, PaginatedList } from '@/lib/types';

const ENTITES = ['Commande', 'Devis', 'Paiement', 'Livraison', 'Remboursement', 'Avoir', 'TicketSAV', 'StockProduit', 'EcartReception', 'Entrepot', 'ParametresEntreprise'];
const TOUTES = 'toutes';

interface Filtres {
    entite: string;
    action: string;
    debut: string;
    fin: string;
    systeme: boolean;
}

const VIDE: Filtres = { entite: TOUTES, action: '', debut: '', fin: '', systeme: false };

/** « CreationCommande » → « Creation commande » : lisible sans table de traduction. */
function action(nom: string): string {
    const mots = nom.replace(/([a-z])([A-Z])/g, '$1 $2').toLowerCase();
    return mots.charAt(0).toUpperCase() + mots.slice(1);
}

function Json({ titre, valeur }: { titre: string; valeur: string | null }) {
    let contenu = valeur;
    try {
        contenu = valeur ? JSON.stringify(JSON.parse(valeur), null, 2) : null;
    } catch {
        /* texte brut */
    }
    return (
        <div className="min-w-0 flex-1">
            <div className="text-muted-foreground mb-1 text-xs font-medium uppercase">{titre}</div>
            <pre className="bg-muted max-h-64 overflow-auto rounded-md p-3 text-xs whitespace-pre-wrap">{contenu ?? '—'}</pre>
        </div>
    );
}

/** Journal d'audit (§32, §45) : QUI, QUAND, QUOI, AVANT, APRÈS. Lecture seule, inviolable. */
export function AuditPage() {
    const [saisie, setSaisie] = useState<Filtres>(VIDE);
    const [filtres, setFiltres] = useState<Filtres>(VIDE);
    const [page, setPage] = useState(1);
    const [ouverte, setOuverte] = useState<string | null>(null);

    const { data, isPending, error } = useQuery({
        queryKey: ['audit', filtres, page],
        queryFn: () => api.get<PaginatedList<JournalAudit>>('/audit', {
            entite: filtres.entite === TOUTES ? undefined : filtres.entite,
            action: filtres.action.trim() || undefined,
            debut: filtres.debut ? `${filtres.debut}T00:00:00Z` : undefined,
            fin: filtres.fin ? `${filtres.fin}T23:59:59Z` : undefined,
            systeme: filtres.systeme || undefined,
            pageNumber: page,
            pageSize: 50,
        }),
    });

    const appliquer = (e: FormEvent) => { e.preventDefault(); setFiltres(saisie); setPage(1); };

    return (
        <>
            <EnTetePage titre="Journal d'audit" description="Qui a fait quoi, quand, avec l'état avant et après. Le journal ne peut être ni modifié ni supprimé." />

            <Card className="mb-4">
                <CardContent className="pt-6">
                    <form onSubmit={appliquer} className="grid gap-3 sm:grid-cols-2 lg:grid-cols-6 lg:items-end">
                        <div className="grid gap-2 lg:col-span-1">
                            <Label>Entité</Label>
                            <Select value={saisie.entite} onValueChange={v => setSaisie(s => ({ ...s, entite: v }))}>
                                <SelectTrigger><SelectValue /></SelectTrigger>
                                <SelectContent>
                                    <SelectItem value={TOUTES}>Toutes</SelectItem>
                                    {ENTITES.map(e => <SelectItem key={e} value={e}>{e}</SelectItem>)}
                                </SelectContent>
                            </Select>
                        </div>
                        <div className="grid gap-2 lg:col-span-2">
                            <Label htmlFor="action">Action exacte</Label>
                            <Input id="action" placeholder="Ex : ConfirmationPaiement" value={saisie.action} onChange={e => setSaisie(s => ({ ...s, action: e.target.value }))} />
                        </div>
                        <div className="grid gap-2">
                            <Label htmlFor="debut">Du</Label>
                            <Input id="debut" type="date" value={saisie.debut} onChange={e => setSaisie(s => ({ ...s, debut: e.target.value }))} />
                        </div>
                        <div className="grid gap-2">
                            <Label htmlFor="fin">Au</Label>
                            <Input id="fin" type="date" value={saisie.fin} onChange={e => setSaisie(s => ({ ...s, fin: e.target.value }))} />
                        </div>
                        <div className="flex gap-2">
                            <Button type="button" variant={saisie.systeme ? 'default' : 'outline'} title="Actions automatiques uniquement"
                                onClick={() => setSaisie(s => ({ ...s, systeme: !s.systeme }))}>
                                <Bot /> Système
                            </Button>
                            <Button type="submit"><Search /> Filtrer</Button>
                        </div>
                    </form>
                </CardContent>
            </Card>

            <Card>
                <CardContent className="p-0">
                    {isPending ? <div className="p-4"><Chargement lignes={8} /></div> : error ? <div className="p-4"><EtatErreur erreur={error} /></div>
                        : data.items.length === 0 ? <EtatVide message="Aucune entrée ne correspond." /> : (
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    <TableHead className="w-8" />
                                    <TableHead>Quand</TableHead>
                                    <TableHead>Qui</TableHead>
                                    <TableHead>Quoi</TableHead>
                                    <TableHead>Sur</TableHead>
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                {data.items.map(j => (
                                    <Fragment key={j.id}>
                                        <TableRow className="cursor-pointer" onClick={() => setOuverte(o => (o === j.id ? null : j.id))}>
                                            <TableCell>{ouverte === j.id ? <ChevronDown className="size-4" /> : <ChevronRight className="size-4" />}</TableCell>
                                            <TableCell className="whitespace-nowrap">{formatDate(j.dateAction)}</TableCell>
                                            <TableCell>
                                                {j.utilisateurId ? j.auteur : <span className="text-muted-foreground inline-flex items-center gap-1"><Bot className="size-4" /> Système</span>}
                                                {j.adresseIP && <div className="text-muted-foreground font-mono text-xs">{j.adresseIP}</div>}
                                            </TableCell>
                                            <TableCell className="font-medium">{action(j.action)}</TableCell>
                                            <TableCell><div>{j.entite}</div><div className="text-muted-foreground font-mono text-xs">{j.entiteId.slice(0, 8)}…</div></TableCell>
                                        </TableRow>
                                        {ouverte === j.id && (
                                            <TableRow className="hover:bg-transparent">
                                                <TableCell />
                                                <TableCell colSpan={4}>
                                                    <div className="flex flex-col gap-3 py-2 md:flex-row">
                                                        <Json titre="Avant" valeur={j.avant} />
                                                        <Json titre="Après" valeur={j.apres} />
                                                    </div>
                                                </TableCell>
                                            </TableRow>
                                        )}
                                    </Fragment>
                                ))}
                            </TableBody>
                        </Table>
                    )}
                </CardContent>
            </Card>
            <div className="flex items-center justify-between">
                <p className="text-muted-foreground mt-4 flex items-center gap-1 text-xs">
                    <Lock className="size-3" /> {data ? `${data.totalCount} entrée(s)` : ''} · protégé en base contre toute modification
                </p>
                {data && <Pagination page={page} totalPages={data.totalPages} surChangement={setPage} />}
            </div>
        </>
    );
}
