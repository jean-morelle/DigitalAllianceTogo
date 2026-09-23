/** Types des réponses de l'API (miroir des DTO C#, en camelCase). */

export interface PaginatedList<T> {
    items: T[];
    pageNumber: number;
    totalPages: number;
    totalCount: number;
    hasPreviousPage: boolean;
    hasNextPage: boolean;
}

// ---------- Tableau de bord ----------

export interface FileDeTravail {
    cle: string;
    libelle: string;
    responsable: string;
    nombre: number;
    plusAncien: string | null;
}

export interface Statistiques {
    debut: string;
    fin: string;
    ventes: {
        commandesCreees: number;
        paiementsConfirmes: number;
        encaisse: number;
        rembourse: number;
        encaisseNet: number;
        panierMoyen: number;
        commandesLivrees: number;
        commandesAnnulees: number;
    };
    devis: {
        crees: number;
        acceptes: number;
        refuses: number;
        expires: number;
        enCours: number;
        tauxAcceptationPourcent: number;
        remiseMoyennePourcent: number;
        remisesSoumisesAdmin: number;
    };
    acquisitionParSource: { source: string; nouveauxClients: number; clientsAyantPaye: number; tauxConversionPourcent: number; encaisse: number }[];
    topProduitsLivres: { produitId: string; reference: string; nom: string; quantite: number; montant: number }[];
    livraisons: {
        remises: number;
        livrees: number;
        livreesAvecReserve: number;
        aReprogrammer: number;
        refusClient: number;
        tauxReussitePourcent: number;
        delaiMoyenPaiementLivraisonHeures: number | null;
    };
    sav: { ouverts: number; clotures: number; repares: number; remplaces: number; rembourses: number; avoirs: number; delaiMoyenResolutionJours: number | null };
    instantane: {
        commandesEnAttenteStock: number;
        produitsSousSeuil: number;
        unitesDefectueuses: number;
        unitesEnTransit: number;
        ticketsSavEnCours: number;
        remboursementsNonRegles: number;
        avoirsDisponibles: number;
    };
}

// ---------- Commandes / paiements ----------

export interface Paiement {
    id: string;
    reference: string;
    montant: number;
    statut: string;
    mode: string;
    datePaiement: string;
    referenceExterne: string | null;
    preuveUrl: string | null;
    dateConfirmation: string | null;
    motifRejet: string | null;
    numeroVersion: number;
    commandeId: string;
    commandeReference: string;
}

export interface Commande {
    id: string;
    reference: string;
    statut: string;
    dateCreation: string;
    versionActive: number;
    total: number;
    clientId: string;
    codeClient: string;
}

export interface LigneCommande {
    id: string;
    produitId: string;
    produitReference: string;
    produitNom: string;
    quantite: number;
    prixUnitaire: number;
    remise: number;
    total: number;
}

export interface VersionCommande {
    id: string;
    numeroVersion: number;
    statut: string;
    active: boolean;
    dateCreation: string;
    motifModification: string | null;
    motifRefus: string | null;
    dateReponse: string | null;
    sousTotal: number;
    remise: number;
    total: number;
    lignes: LigneCommande[];
}

export interface Remboursement {
    id: string;
    reference: string;
    montant: number;
    statut: string;
    motif: string;
    dateDemande: string;
    dateExecution: string | null;
    referenceTransaction: string | null;
    motifEchec: string | null;
    commandeId: string;
    commandeReference: string;
    numeroVersion: number;
}

export interface Avoir {
    id: string;
    reference: string;
    montant: number;
    montantUtilise: number;
    montantRestant: number;
    statut: string;
    motif: string;
    dateCreation: string;
    dateUtilisation: string | null;
    commandeId: string;
    commandeReference: string;
    numeroVersion: number;
}

export interface CommandeDetail extends Commande {
    sousTotal: number;
    remise: number;
    devisOrigineId: string | null;
    adresseLivraison: { ligne1: string; ligne2: string | null; ville: string; pays: string; codePostal: string; telephoneContact: string };
    lignes: LigneCommande[];
    paiements: Paiement[];
    remboursements: Remboursement[];
    avoirs: Avoir[];
    resteAPayer: number;
    versions: VersionCommande[];
    livraisons: SuiviLivraison[];
    dateLimitePaiement: string | null;
}

export interface JournalAudit {
    id: string;
    dateAction: string;
    action: string;
    entite: string;
    entiteId: string;
    utilisateurId: string | null;
    auteur: string;
    adresseIP: string | null;
    avant: string | null;
    apres: string | null;
}

// ---------- Devis / clients / produits ----------

export interface Devis {
    id: string;
    reference: string;
    statut: string;
    dateCreation: string;
    dateValidite: string;
    sousTotal: number;
    remise: number;
    total: number;
    valideParEntreprise: boolean;
    clientId: string;
    codeClient: string;
}

export interface LigneDevis {
    id: string;
    produitId: string;
    produitReference: string;
    produitNom: string;
    quantite: number;
    prixUnitaire: number;
    remise: number;
    total: number;
}

export interface DevisDetail extends Devis {
    tauxRemise: number;
    valideParId: string | null;
    dateValidation: string | null;
    creeParId: string | null;
    commentaireClient: string | null;
    commentaireInterne: string | null;
    commandeId: string | null;
    lignes: LigneDevis[];
}

/** Ligne saisie dans un formulaire (devis ou modification de commande). */
export interface LigneSaisie {
    produitId: string;
    quantite: number;
    remise: number;
}

export interface Produit {
    id: string;
    reference: string;
    nom: string;
    prix: number;
    actif: boolean;
}

export interface Client {
    id: string;
    codeClient: string;
    type: string;
    nom: string;
    prenom: string | null;
    raisonSociale: string | null;
    telephone: string;
    email: string | null;
    source: string;
    dateCreation: string;
}

export interface Adresse {
    id: string;
    libelle: string;
    ligne1: string;
    ligne2: string | null;
    ville: string;
    pays: string;
    codePostal: string;
}

export interface ClientDetail extends Client {
    adresses: Adresse[];
}

// ---------- Stock ----------

export interface StockProduit {
    id: string;
    produitId: string;
    produitReference: string;
    produitNom: string;
    entrepotId: string;
    entrepotNom: string;
    quantitePhysique: number;
    quantiteReservee: number;
    quantiteDisponible: number;
    quantiteEnTransit: number;
    quantiteDefectueuse: number;
    seuilAlerte: number;
}

export interface Entrepot {
    id: string;
    nom: string;
    adresse: string;
    actif: boolean;
}

export interface MouvementStock {
    id: string;
    type: string;
    quantite: number;
    dateMouvement: string;
    motif: string;
    reference: string;
    commandeId: string | null;
    commandeReference: string | null;
    ticketSAVId: string | null;
}

export interface EcartReception {
    id: string;
    reference: string;
    produitId: string;
    produitNom: string;
    entrepotId: string;
    entrepotNom: string;
    quantiteCommandee: number;
    quantiteRecue: number;
    surplus: number;
    statut: string;
    dateConstat: string;
    dateDecision: string | null;
    motifDecision: string | null;
}

export interface ResultatEntreeStock {
    quantitePhysique: number;
    quantiteDisponible: number;
    commandesReservees: string[];
    surplusEnAttente: number;
    ecartId: string | null;
}

// ---------- Livraisons ----------

export interface Livraison {
    id: string;
    reference: string;
    type: string;
    statut: string;
    datePlanifiee: string;
    datePriseEnCharge: string | null;
    dateLivraison: string | null;
    motifEchec: string | null;
    reserve: string | null;
    commandeId: string;
    commandeReference: string;
    ticketSAVId: string | null;
    livreurId: string | null;
    livreurNom: string | null;
    adresseLigne1: string;
    adresseLigne2: string | null;
    ville: string;
    telephoneContact: string;
    preuve: { datePreuve: string; photoUrl: string | null; signatureUrl: string | null; latitude: number | null; longitude: number | null; commentaire: string | null } | null;
}

export interface Livreur {
    id: string;
    nom: string;
    telephone: string;
    livraisonsEnCours: number;
}

// ---------- SAV ----------

export interface TicketSav {
    id: string;
    reference: string;
    statut: string;
    dateCreation: string;
    motif: string;
    quantite: number;
    decision: string | null;
    resolution: string | null;
    dateCloture: string | null;
    ancienProduitReceptionne: boolean;
    clientId: string;
    codeClient: string;
    commandeId: string;
    commandeReference: string;
    ligneCommandeId: string;
    produitId: string;
    produitNom: string;
    technicienId: string | null;
}

export interface SuiviSav {
    id: string;
    type: string;
    reference: string;
    statut: string;
    montant: number | null;
}

export interface TicketSavDetail extends TicketSav {
    diagnostics: { date: string; conclusion: string; reparable: boolean; recommandation: string | null; technicienId: string }[];
    interventions: { dateDebut: string; dateFin: string | null; description: string; resultat: string | null; technicienId: string }[];
    livraisons: SuiviSav[];
    regularisations: SuiviSav[];
}

// ---------- Espace client ----------

export interface ProduitCatalogue {
    id: string;
    reference: string;
    nom: string;
    prix: number;
    actif: boolean;
    categorieNom: string;
    marqueNom: string;
    imagePrincipaleUrl: string | null;
    enStock: boolean;
}

export interface ProduitDetail extends Omit<ProduitCatalogue, 'imagePrincipaleUrl'> {
    description: string;
    categorieId: string;
    marqueId: string;
    images: { id: string; url: string; ordre: number; estPrincipale: boolean }[];
    attributs: { id: string; cle: string; valeur: string; ordre: number }[];
}

/** Catégorie ou marque. */
export interface Categorie {
    id: string;
    nom: string;
    description?: string;
    actif?: boolean;
}

export interface LignePanier {
    produitId: string;
    reference: string;
    nom: string;
    imageUrl: string | null;
    prixUnitaire: number;
    quantite: number;
    total: number;
    quantiteDisponible: number;
    actif: boolean;
}

export interface Panier {
    lignes: LignePanier[];
    nombreArticles: number;
    total: number;
    toutDisponible: boolean;
}

export interface SuiviLivraison {
    reference: string;
    type: string;
    statut: string;
    datePlanifiee: string;
    dateLivraison: string | null;
    motifEchec: string | null;
}

export interface Parametres {
    seuilRemiseCommercialPourcent: number;
    seuilAugmentationModificationPourcent: number;
    delaiExpirationPaiementHeures: number;
    dureeValiditeDevisJours: number;
    numeroTMoney: string | null;
    numeroFlooz: string | null;
    nomBeneficiairePaiement: string | null;
    dateModification: string;
}

export interface InfosPaiement {
    numeroTMoney: string | null;
    numeroFlooz: string | null;
    nomBeneficiaire: string | null;
    delaiPaiementHeures: number;
}

export interface NotificationClient {
    id: string;
    dateCreation: string;
    type: string;
    titre: string;
    message: string;
    lien: string | null;
    lue: boolean;
}
