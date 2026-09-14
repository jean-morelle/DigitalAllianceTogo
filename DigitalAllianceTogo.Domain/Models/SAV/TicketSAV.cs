using DigitalAllianceTogo.Domain.Enum;

namespace DigitalAllianceTogo.Domain.Models.SAV
{
    public class TicketSAV
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
        public string Motif { get; set; } = string.Empty;
        public StatutSav Statut { get; set; } = StatutSav.Ouvert;

        public Guid ClientId { get; set; }
        public Models.Security.Client Client { get; set; } = null!;

        // TicketSAV (0..*) --> (1) LigneCommande : produit concerné
        public Guid LigneCommandeId { get; set; }
        public Models.Commande.LigneCommande LigneCommande { get; set; } = null!;

        public ICollection<Diagnostic> Diagnostics { get; set; } = new List<Diagnostic>();
        public ICollection<Intervention> Interventions { get; set; } = new List<Intervention>();
    }
}
