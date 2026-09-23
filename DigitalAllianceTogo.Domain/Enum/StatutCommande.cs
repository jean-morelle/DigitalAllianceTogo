namespace DigitalAllianceTogo.Domain.Enum
{
    public enum StatutCommande
    {
        CommandeCreee = 1,
        PaiementEnAttente = 2,
        PaiementEchoue = 3,
        PaiementConfirme = 4,
        EnAttenteDisponibilite = 5,
        StockReserve = 6,
        PreparationEnCours = 7,
        PretePourLivraison = 8,
        EnTransit = 9,
        Livree = 10,
        LivraisonEchoueeRefusClient = 11,
        AnnulationEnCours = 12, // annulation pendant la préparation : le stock doit récupérer/contrôler les produits
        Annulee = 13,
        EnAttenteRegulationFinanciere = 14,
        Cloturee = 15
    }
}
