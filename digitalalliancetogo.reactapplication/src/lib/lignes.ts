import type { Client, LigneSaisie } from '@/lib/types';

/** Ligne en cours d'édition : ce qui part à l'API + ce qu'il faut pour l'afficher. */
export interface LigneEdition extends LigneSaisie {
    nom: string;
    reference: string;
    prixUnitaire: number;
}

/** Même calcul que l'API : sous-total avant remise, remises de lignes + remise globale. */
export function calculerTotaux(lignes: LigneEdition[], remiseGlobale: number) {
    const sousTotal = lignes.reduce((s, l) => s + l.prixUnitaire * l.quantite, 0);
    const remise = lignes.reduce((s, l) => s + l.remise, 0) + remiseGlobale;
    return { sousTotal, remise, total: sousTotal - remise, taux: sousTotal === 0 ? 0 : (remise * 100) / sousTotal };
}

export function nomClient(c: Pick<Client, 'nom' | 'prenom' | 'raisonSociale'>): string {
    return c.raisonSociale || [c.prenom, c.nom].filter(Boolean).join(' ');
}
