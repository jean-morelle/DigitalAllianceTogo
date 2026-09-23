using DigitalAllianceTogo.Domain.Enum;

namespace DigitalAllianceTogo.Domain.Models.Finance
{
    public class Remboursement
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public decimal Montant { get; set; }
        public DateTime DateDemande { get; set; } = DateTime.UtcNow;
        public DateTime? DateExecution { get; set; }
        public StatutRemboursement Statut { get; set; } = StatutRemboursement.EnAttente;

        // Pourquoi on rembourse (annulation, refus de livraison...)
        public string Motif { get; set; } = string.Empty;

        // Preuve de l'exécution (référence du transfert Mobile Money / virement)
        public string? ReferenceTransaction { get; set; }

        // Raison du dernier échec d'exécution (numéro invalide, compte fermé...)
        public string? MotifEchec { get; set; }

        // Remboursement décidé dans le cadre d'un SAV
        public Guid? TicketSAVId { get; set; }
        public Models.SAV.TicketSAV? TicketSAV { get; set; }

        public Guid CommandeId { get; set; }
        public Models.Commande.Commande Commande { get; set; } = null!;

        // Remboursement (*) --> (1) VersionCommande : concerne
        public Guid VersionCommandeId { get; set; }
        public Models.Commande.VersionCommande VersionCommande { get; set; } = null!;
    }
}
