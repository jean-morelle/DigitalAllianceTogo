import { useEffect, useState, type ReactNode } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Check, ChevronsUpDown } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from '@/components/ui/command';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { api } from '@/lib/api';
import { formatFcfa } from '@/lib/format';
import { nomClient } from '@/lib/lignes';
import type { Client, PaginatedList, Produit } from '@/lib/types';
import { cn } from '@/lib/utils';

function useDiffere<T>(valeur: T, delai = 300): T {
    const [differee, setDifferee] = useState(valeur);
    useEffect(() => {
        const minuteur = window.setTimeout(() => setDifferee(valeur), delai);
        return () => window.clearTimeout(minuteur);
    }, [valeur, delai]);
    return differee;
}

interface SelecteurProps<T> {
    cle: string;
    chemin: string;
    parametres?: Record<string, string | boolean>;
    placeholder: string;
    libelleSelection: string | null;
    exclure?: string[];
    surChoix: (element: T) => void;
    rendu: (element: T) => ReactNode;
    selectionneId?: string;
    className?: string;
}

/** Liste déroulante avec recherche côté serveur (produits, clients...). */
function Selecteur<T extends { id: string }>({
    cle, chemin, parametres, placeholder, libelleSelection, exclure = [], surChoix, rendu, selectionneId, className,
}: SelecteurProps<T>) {
    const [ouvert, setOuvert] = useState(false);
    const [recherche, setRecherche] = useState('');
    const terme = useDiffere(recherche.trim());

    const { data, isFetching } = useQuery({
        queryKey: [cle, terme, parametres],
        queryFn: () => api.get<PaginatedList<T>>(chemin, { ...parametres, recherche: terme, pageSize: 20 }),
        enabled: ouvert,
    });
    const elements = (data?.items ?? []).filter(e => !exclure.includes(e.id));

    return (
        <Popover open={ouvert} onOpenChange={setOuvert}>
            <PopoverTrigger asChild>
                <Button variant="outline" role="combobox" aria-expanded={ouvert} className={cn('w-full justify-between font-normal', className)}>
                    <span className={cn('truncate', !libelleSelection && 'text-muted-foreground')}>{libelleSelection ?? placeholder}</span>
                    <ChevronsUpDown className="opacity-50" />
                </Button>
            </PopoverTrigger>
            <PopoverContent className="w-(--radix-popover-trigger-width) min-w-72 p-0" align="start">
                <Command shouldFilter={false}>
                    <CommandInput placeholder="Rechercher..." value={recherche} onValueChange={setRecherche} />
                    <CommandList>
                        <CommandEmpty>{isFetching ? 'Recherche...' : 'Aucun résultat.'}</CommandEmpty>
                        <CommandGroup>
                            {elements.map(e => (
                                <CommandItem key={e.id} value={e.id} onSelect={() => { surChoix(e); setOuvert(false); setRecherche(''); }}>
                                    <Check className={cn(selectionneId === e.id ? 'opacity-100' : 'opacity-0')} />
                                    {rendu(e)}
                                </CommandItem>
                            ))}
                        </CommandGroup>
                    </CommandList>
                </Command>
            </PopoverContent>
        </Popover>
    );
}

export function SelecteurProduit(props: { exclure?: string[]; surChoix: (p: Produit) => void; libelleSelection?: string | null; className?: string }) {
    return (
        <Selecteur<Produit>
            cle="produits-recherche"
            chemin="/produits"
            parametres={{ actif: true }}
            placeholder="Ajouter un produit..."
            libelleSelection={props.libelleSelection ?? null}
            exclure={props.exclure}
            surChoix={props.surChoix}
            className={props.className}
            rendu={p => (
                <div className="flex w-full items-center justify-between gap-3">
                    <div className="min-w-0"><div className="truncate">{p.nom}</div><div className="text-muted-foreground text-xs">{p.reference}</div></div>
                    <span className="text-muted-foreground shrink-0 text-xs tabular-nums">{formatFcfa(p.prix)}</span>
                </div>
            )}
        />
    );
}

export function SelecteurClient({ client, surChoix }: { client: Client | null; surChoix: (c: Client) => void }) {
    return (
        <Selecteur<Client>
            cle="clients-recherche"
            chemin="/clients"
            placeholder="Choisir un client (nom, téléphone, code)..."
            libelleSelection={client ? `${nomClient(client)} · ${client.codeClient}` : null}
            selectionneId={client?.id}
            surChoix={surChoix}
            rendu={c => (
                <div className="min-w-0">
                    <div className="truncate">{nomClient(c)}</div>
                    <div className="text-muted-foreground text-xs">{c.codeClient} · {c.telephone} · {c.source}</div>
                </div>
            )}
        />
    );
}
