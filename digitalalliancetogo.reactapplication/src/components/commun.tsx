import type { ReactNode } from 'react';
import { AlertCircle, Inbox } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Skeleton } from '@/components/ui/skeleton';
import { libelle, ton, type Ton } from '@/lib/format';
import { cn } from '@/lib/utils';

const CLASSES_TON: Record<Ton, string> = {
    succes: 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300',
    attention: 'bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-300',
    danger: 'bg-red-100 text-red-800 dark:bg-red-950 dark:text-red-300',
    info: 'bg-sky-100 text-sky-800 dark:bg-sky-950 dark:text-sky-300',
    neutre: 'bg-muted text-muted-foreground',
};

/** Statut métier (valeur d'enum de l'API) affiché en français, coloré selon sa nature. */
export function StatutBadge({ statut, className }: { statut: string | null | undefined; className?: string }) {
    return (
        <Badge variant="secondary" className={cn('border-transparent font-medium', CLASSES_TON[ton(statut)], className)}>
            {libelle(statut)}
        </Badge>
    );
}

export function EnTetePage({ titre, description, actions }: { titre: string; description?: string; actions?: ReactNode }) {
    return (
        <div className="mb-6 flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
            <div>
                <h1 className="text-2xl font-semibold tracking-tight">{titre}</h1>
                {description && <p className="text-muted-foreground mt-1 text-sm">{description}</p>}
            </div>
            {actions && <div className="flex flex-wrap gap-2">{actions}</div>}
        </div>
    );
}

export function EtatErreur({ erreur }: { erreur: unknown }) {
    return (
        <Alert variant="destructive">
            <AlertCircle />
            <AlertTitle>Chargement impossible</AlertTitle>
            <AlertDescription>{erreur instanceof Error ? erreur.message : 'Erreur inconnue.'}</AlertDescription>
        </Alert>
    );
}

export function EtatVide({ message }: { message: string }) {
    return (
        <div className="text-muted-foreground flex flex-col items-center gap-2 py-12 text-sm">
            <Inbox className="size-8 opacity-50" />
            {message}
        </div>
    );
}

export function Chargement({ lignes = 4 }: { lignes?: number }) {
    return (
        <div className="space-y-3">
            {Array.from({ length: lignes }, (_, i) => <Skeleton key={i} className="h-10 w-full" />)}
        </div>
    );
}
