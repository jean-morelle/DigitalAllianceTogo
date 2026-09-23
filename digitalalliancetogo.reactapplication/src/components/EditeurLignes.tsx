import { AlertTriangle, Trash2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Table, TableBody, TableCell, TableFooter, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { SelecteurProduit } from '@/components/Selecteurs';
import { formatFcfa } from '@/lib/format';
import { calculerTotaux, type LigneEdition } from '@/lib/lignes';

/** Seuil par défaut (ParametresEntreprise) au-delà duquel l'Administrateur valide la remise. */
const SEUIL_REMISE_POURCENT = 10;

interface Props {
    lignes: LigneEdition[];
    remiseGlobale: number;
    surChangement: (lignes: LigneEdition[], remiseGlobale: number) => void;
    /** Montant de référence (version active d'une commande) pour afficher l'écart. */
    totalReference?: number;
}

export function EditeurLignes({ lignes, remiseGlobale, surChangement, totalReference }: Props) {
    const { sousTotal, remise, total, taux } = calculerTotaux(lignes, remiseGlobale);
    const majLigne = (index: number, champ: Partial<LigneEdition>) =>
        surChangement(lignes.map((l, i) => (i === index ? { ...l, ...champ } : l)), remiseGlobale);

    return (
        <div className="space-y-4">
            <div className="overflow-x-auto rounded-md border">
                <Table>
                    <TableHeader>
                        <TableRow>
                            <TableHead className="min-w-48">Produit</TableHead>
                            <TableHead className="text-right">Prix unitaire</TableHead>
                            <TableHead className="w-24">Qté</TableHead>
                            <TableHead className="w-36">Remise ligne</TableHead>
                            <TableHead className="text-right">Total</TableHead>
                            <TableHead className="w-10" />
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {lignes.length === 0 && (
                            <TableRow><TableCell colSpan={6} className="text-muted-foreground py-6 text-center">Ajoutez au moins un produit.</TableCell></TableRow>
                        )}
                        {lignes.map((l, i) => {
                            const brut = l.prixUnitaire * l.quantite;
                            return (
                                <TableRow key={l.produitId}>
                                    <TableCell><div className="font-medium">{l.nom}</div><div className="text-muted-foreground text-xs">{l.reference}</div></TableCell>
                                    <TableCell className="text-right tabular-nums">{formatFcfa(l.prixUnitaire)}</TableCell>
                                    <TableCell>
                                        <Input type="number" min={1} value={l.quantite} aria-label={`Quantité ${l.nom}`}
                                            onChange={e => majLigne(i, { quantite: Math.max(1, Math.floor(Number(e.target.value) || 1)) })} />
                                    </TableCell>
                                    <TableCell>
                                        <Input type="number" min={0} step={500} value={l.remise} aria-label={`Remise ${l.nom}`}
                                            className={l.remise > brut ? 'border-destructive' : undefined}
                                            onChange={e => majLigne(i, { remise: Math.max(0, Number(e.target.value) || 0) })} />
                                    </TableCell>
                                    <TableCell className="text-right font-medium tabular-nums">{formatFcfa(brut - l.remise)}</TableCell>
                                    <TableCell>
                                        <Button variant="ghost" size="icon" aria-label={`Retirer ${l.nom}`}
                                            onClick={() => surChangement(lignes.filter((_, j) => j !== i), remiseGlobale)}>
                                            <Trash2 />
                                        </Button>
                                    </TableCell>
                                </TableRow>
                            );
                        })}
                    </TableBody>
                    <TableFooter>
                        <TableRow>
                            <TableCell colSpan={4} className="text-right">Sous-total</TableCell>
                            <TableCell className="text-right tabular-nums">{formatFcfa(sousTotal)}</TableCell>
                            <TableCell />
                        </TableRow>
                        <TableRow>
                            <TableCell colSpan={4} className="text-right">Remise totale ({taux.toFixed(1)} %)</TableCell>
                            <TableCell className="text-right tabular-nums">− {formatFcfa(remise)}</TableCell>
                            <TableCell />
                        </TableRow>
                        <TableRow>
                            <TableCell colSpan={4} className="text-right font-semibold">Total</TableCell>
                            <TableCell className="text-right text-base font-semibold tabular-nums">{formatFcfa(total)}</TableCell>
                            <TableCell />
                        </TableRow>
                        {totalReference !== undefined && total !== totalReference && (
                            <TableRow>
                                <TableCell colSpan={4} className="text-muted-foreground text-right">Écart avec la version actuelle</TableCell>
                                <TableCell className="text-right tabular-nums">{total > totalReference ? '+' : '−'} {formatFcfa(Math.abs(total - totalReference))}</TableCell>
                                <TableCell />
                            </TableRow>
                        )}
                    </TableFooter>
                </Table>
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
                <div className="grid gap-2">
                    <Label>Ajouter un produit</Label>
                    <SelecteurProduit
                        exclure={lignes.map(l => l.produitId)}
                        surChoix={p => surChangement([...lignes, { produitId: p.id, nom: p.nom, reference: p.reference, prixUnitaire: p.prix, quantite: 1, remise: 0 }], remiseGlobale)}
                    />
                </div>
                <div className="grid gap-2">
                    <Label htmlFor="remiseGlobale">Remise globale (FCFA)</Label>
                    <Input id="remiseGlobale" type="number" min={0} step={1000} value={remiseGlobale}
                        onChange={e => surChangement(lignes, Math.max(0, Number(e.target.value) || 0))} />
                </div>
            </div>

            {taux > SEUIL_REMISE_POURCENT && (
                <p className="flex items-center gap-2 text-sm text-amber-700 dark:text-amber-400">
                    <AlertTriangle className="size-4" />
                    Remise de {taux.toFixed(1)} % : au-delà de {SEUIL_REMISE_POURCENT} %, la validation de l'Administrateur sera nécessaire.
                </p>
            )}
            {remise > sousTotal && <p className="text-destructive text-sm">La remise dépasse le montant.</p>}
        </div>
    );
}
