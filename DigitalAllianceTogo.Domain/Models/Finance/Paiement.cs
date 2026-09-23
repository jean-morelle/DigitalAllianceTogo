using DigitalAllianceTogo.Domain.Enum;

namespace DigitalAllianceTogo.Domain.Models.Finance
{
    public class Paiement
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public decimal Montant { get; set; }
        public DateTime DatePaiement { get; set; } = DateTime.UtcNow;
        public StatutPaiement Statut { get; set; } = StatutPaiement.EnAttente;
        public ModePaiement Mode { get; set; }

        // Paiement externe : preuve fournie par le client (référence de transaction, capture...)
        public string? ReferenceExterne { get; set; }
        public string? PreuveUrl { get; set; }

        // Traçabilité de la confirmation : un paiement externe ne peut pas être
        // confirmé sans savoir QUI l'a vérifié et QUAND.
        public Guid? ConfirmeParId { get; set; }
        public Models.Security.Utilisateur? ConfirmePar { get; set; }
        public DateTime? DateConfirmation { get; set; }

        public Guid CommandeId { get; set; }
        public Models.Commande.Commande Commande { get; set; } = null!;

        // Paiement (*) --> (1) VersionCommande : concerne
        public Guid VersionCommandeId { get; set; }
        public Models.Commande.VersionCommande VersionCommande { get; set; } = null!;
    }
}
