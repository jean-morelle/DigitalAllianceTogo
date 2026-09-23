namespace DigitalAllianceTogo.Domain.Enum
{
    /// <summary>
    /// Cycle d'une version de commande (§20). La version 1 (issue du devis) est Acceptée
    /// d'emblée ; une modification après paiement est une nouvelle version proposée.
    /// </summary>
    public enum StatutVersionCommande
    {
        Acceptee = 1,
        EnValidationAdmin = 2,   // hausse au-delà du seuil ou remise exceptionnelle
        EnAttenteClient = 3,
        Refusee = 4,             // par le client ou l'Administrateur
        Retiree = 5              // retirée par le commercial, ou devenue caduque (commande annulée / partie en livraison)
    }
}
