namespace DigitalAllianceTogo.Domain.Enum
{
    public enum StatutLivraison
    {
        Planifiee = 1,
        PriseEnCharge = 2,
        EnTransit = 3,
        Livree = 4,
        LivreeAvecReserve = 5, // colis accepté mais anomalie signalée (ne crée PAS de ticket SAV automatiquement)
        Echouee = 6,
        AReprogrammer = 7,     // échec permettant une nouvelle tentative (état de livraison, pas de commande)
        Retournee = 8,
    }
}
