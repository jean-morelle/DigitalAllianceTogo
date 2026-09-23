namespace DigitalAllianceTogo.Domain.Models.Parametres
{
    /// <summary>
    /// Paramètres métier configurables par l'Administrateur.
    /// Une seule ligne en base : les règles métier lisent ces valeurs
    /// au lieu d'avoir des seuils codés en dur.
    /// </summary>
    public class ParametresEntreprise
    {
        public Guid Id { get; set; }

        // Remise au-delà de laquelle la validation de l'Administrateur est obligatoire (en %)
        public decimal SeuilRemiseCommercialPourcent { get; set; } = 10m;

        // Augmentation de prix (modification de commande) au-delà de laquelle l'Administrateur valide (en %)
        public decimal SeuilAugmentationModificationPourcent { get; set; } = 10m;

        // Délai pendant lequel un client peut réessayer un paiement échoué avant annulation
        public int DelaiExpirationPaiementHeures { get; set; } = 48;

        // Durée de validité par défaut d'un devis envoyé
        public int DureeValiditeDevisJours { get; set; } = 15;

        // Numéros Mobile Money sur lesquels les clients envoient leurs paiements (affichés au client)
        public string? NumeroTMoney { get; set; }
        public string? NumeroFlooz { get; set; }

        // Nom affiché par l'opérateur au moment du transfert : le client vérifie qu'il paie le bon destinataire
        public string? NomBeneficiairePaiement { get; set; }

        public DateTime DateModification { get; set; } = DateTime.UtcNow;
    }
}
