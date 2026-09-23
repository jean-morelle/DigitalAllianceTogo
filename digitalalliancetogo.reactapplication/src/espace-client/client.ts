import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';
import type { Adresse, ClientDetail } from '@/lib/types';

/** Fiche du client connecté (adresses, téléphone). */
export function useMaFiche() {
    return useQuery({ queryKey: ['ma-fiche'], queryFn: () => api.get<ClientDetail>('/clients/moi') });
}

/** Adresse retenue : celle choisie, sinon la première du client. */
export function adresseParDefaut(adresses: Adresse[] | undefined, choisie: string): string {
    return choisie || adresses?.[0]?.id || '';
}

/** Libellés côté client : plus parlants que les statuts internes. */
export const STATUT_CLIENT: Record<string, string> = {
    CommandeCreee: 'À payer',
    PaiementEnAttente: 'Paiement en vérification',
    PaiementEchoue: 'Paiement refusé',
    PaiementConfirme: 'Payée',
    EnAttenteDisponibilite: 'Payée · en réapprovisionnement',
    StockReserve: 'Payée · en préparation',
    PreparationEnCours: 'En préparation',
    PretePourLivraison: 'Prête à livrer',
    EnTransit: 'En cours de livraison',
    Livree: 'Livrée',
    LivraisonEchoueeRefusClient: 'Refusée à la livraison',
    AnnulationEnCours: 'Annulation en cours',
    Annulee: 'Annulée',
    EnAttenteRegulationFinanciere: 'Remboursement en cours',
    Cloturee: 'Terminée',
};
