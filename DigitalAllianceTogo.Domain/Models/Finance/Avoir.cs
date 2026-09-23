using DigitalAllianceTogo.Domain.Enum;

namespace DigitalAllianceTogo.Domain.Models.Finance
{
    public class Avoir
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public decimal Montant { get; set; }
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
        public DateTime? DateUtilisation { get; set; }
        public StatutAvoir Statut { get; set; } = StatutAvoir.EnAttente;

        // Pourquoi l'avoir est émis (annulation, refus de livraison...) ou annulé
        public string Motif { get; set; } = string.Empty;

        // Avoir décidé dans le cadre d'un SAV
        public Guid? TicketSAVId { get; set; }
        public Models.SAV.TicketSAV? TicketSAV { get; set; }

        public Guid CommandeId { get; set; }
        public Models.Commande.Commande Commande { get; set; } = null!;

        // Avoir (*) --> (1) VersionCommande : concerne
        public Guid VersionCommandeId { get; set; }
        public Models.Commande.VersionCommande VersionCommande { get; set; } = null!;
    }
}
