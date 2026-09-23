/** Mise en forme adaptée au Togo : FCFA sans décimales, dates en français (heure de Lomé = UTC). */

const fcfa = new Intl.NumberFormat('fr-FR', { maximumFractionDigits: 0 });

export function formatFcfa(montant: number | null | undefined): string {
    return montant === null || montant === undefined ? '—' : `${fcfa.format(montant)} FCFA`;
}

export function formatDate(date: string | null | undefined, avecHeure = true): string {
    if (!date) return '—';
    return new Date(date).toLocaleString('fr-FR', avecHeure
        ? { dateStyle: 'short', timeStyle: 'short' }
        : { dateStyle: 'medium' });
}

/** « il y a 3 j », pour l'ancienneté des éléments à traiter. */
export function depuis(date: string | null | undefined): string {
    if (!date) return '';
    const minutes = Math.floor((Date.now() - new Date(date).getTime()) / 60_000);
    if (minutes < 60) return `il y a ${Math.max(minutes, 1)} min`;
    const heures = Math.floor(minutes / 60);
    if (heures < 24) return `il y a ${heures} h`;
    return `il y a ${Math.floor(heures / 24)} j`;
}

/** Libellés français des statuts renvoyés par l'API (valeurs d'enum en texte). */
const LIBELLES: Record<string, string> = {
    // Commande
    CommandeCreee: 'En attente de paiement',
    PaiementEnAttente: 'Paiement à vérifier',
    PaiementEchoue: 'Paiement échoué',
    PaiementConfirme: 'Paiement confirmé',
    EnAttenteDisponibilite: 'En attente de stock',
    StockReserve: 'Stock réservé',
    PreparationEnCours: 'En préparation',
    PretePourLivraison: 'Prête pour livraison',
    EnTransit: 'En transit',
    LivraisonEchoueeRefusClient: 'Refusée à la livraison',
    AnnulationEnCours: 'Annulation en cours',
    Annulee: 'Annulée',
    EnAttenteRegulationFinanciere: 'Régularisation financière',
    Cloturee: 'Clôturée',
    // Paiement / remboursement / avoir
    EnAttente: 'En attente',
    Confirme: 'Confirmé',
    Echoue: 'Échoué',
    Valide: 'Validé',
    Execute: 'Exécuté',
    Disponible: 'Disponible',
    Utilise: 'Utilisé',
    Annule: 'Annulé',
    // Devis
    Brouillon: 'Brouillon',
    ValidationInterne: 'Validation Admin',
    Envoye: 'Envoyé',
    Accepte: 'Accepté',
    Refuse: 'Refusé',
    ModificationDemandee: 'Modification demandée',
    Expire: 'Expiré',
    // Livraison
    Planifiee: 'Planifiée',
    PriseEnCharge: 'Prise en charge',
    Livree: 'Livrée',
    LivreeAvecReserve: 'Livrée avec réserve',
    Echouee: 'Échouée',
    AReprogrammer: 'À reprogrammer',
    Retournee: 'Retournée',
    // SAV
    Ouvert: 'Ouvert',
    Diagnostique: 'Diagnostiqué',
    EnReparation: 'En réparation',
    DecisionCommerciale: 'Décision commerciale',
    RemplacementEnCours: 'Remplacement en cours',
    Cloture: 'Clôturé',
    // Versions de commande
    Acceptee: 'Acceptée',
    EnValidationAdmin: 'Validation Admin',
    EnAttenteClient: 'Attente client',
    Refusee: 'Refusée',
    Retiree: 'Retirée',
    // Stock : mouvements et écarts fournisseur
    Entree: 'Entrée',
    Sortie: 'Sortie',
    Reservation: 'Réservation',
    Liberation: 'Libération',
    Retour: 'Retour',
    Ajustement: 'Ajustement',
    EnAttenteDecision: 'À décider',
    IntegreAuStock: 'Intégré au stock',
    RetourneFournisseur: 'Retourné au fournisseur',
    // Décisions SAV
    Remplacement: 'Remplacement',
    Remboursement: 'Remboursement',
    // Types de livraison
    Initial: 'Livraison',
    Relivraison: 'Relivraison',
    RemplacementSav: 'Remplacement SAV',
    // Modes
    Externe: 'Mobile Money / virement',
    Integre: 'Paiement en ligne',
    Avoir: 'Avoir',
};

export function libelle(statut: string | null | undefined): string {
    return statut ? LIBELLES[statut] ?? statut : '—';
}

/** Ton visuel d'un statut : succès, attention, danger ou neutre. */
export type Ton = 'succes' | 'attention' | 'danger' | 'info' | 'neutre';

const TONS: Record<string, Ton> = {
    Livree: 'succes', Cloturee: 'succes', Confirme: 'succes', Execute: 'succes', Disponible: 'succes', Accepte: 'succes',
    Acceptee: 'succes', StockReserve: 'succes', PretePourLivraison: 'succes', Cloture: 'succes', PaiementConfirme: 'succes',
    PaiementEnAttente: 'attention', EnAttente: 'attention', EnAttenteDisponibilite: 'attention', ValidationInterne: 'attention',
    EnValidationAdmin: 'attention', EnAttenteClient: 'attention', EnAttenteRegulationFinanciere: 'attention', AReprogrammer: 'attention',
    DecisionCommerciale: 'attention', AnnulationEnCours: 'attention', LivreeAvecReserve: 'attention', CommandeCreee: 'attention',
    PaiementEchoue: 'danger', Echoue: 'danger', Echouee: 'danger', LivraisonEchoueeRefusClient: 'danger', Refuse: 'danger',
    Refusee: 'danger', Expire: 'danger',
    EnAttenteDecision: 'attention', IntegreAuStock: 'succes', Entree: 'succes', Reservation: 'info', Sortie: 'info',
    EnTransit: 'info', PreparationEnCours: 'info', EnReparation: 'info', RemplacementEnCours: 'info', Planifiee: 'info', Envoye: 'info',
};

export function ton(statut: string | null | undefined): Ton {
    return statut ? TONS[statut] ?? 'neutre' : 'neutre';
}
