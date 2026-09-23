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

        // Nombre d'unités de la ligne concernées (ex : 1 PC sur les 3 commandés)
        public int Quantite { get; set; } = 1;

        // Décision commerciale si le produit n'est pas réparable
        public DecisionSav? Decision { get; set; }

        // Comment le ticket s'est terminé (réparé, remplacé, remboursé...)
        public string? Resolution { get; set; }
        public DateTime? DateCloture { get; set; }

        // L'ancien produit a été récupéré et classé par le stock (réutilisable / défectueux)
        public bool AncienProduitReceptionne { get; set; }

        // Jeton de concurrence optimiste (xmin)
        public uint Version { get; set; }

        public Guid ClientId { get; set; }
        public Models.Security.Client Client { get; set; } = null!;

        // TicketSAV (0..*) --> (1) LigneCommande : produit concerné
        public Guid LigneCommandeId { get; set; }
        public Models.Commande.LigneCommande LigneCommande { get; set; } = null!;

        // Dernier technicien ayant pris le ticket en charge
        public Guid? TechnicienId { get; set; }
        public Models.Security.Utilisateur? Technicien { get; set; }

        public ICollection<Diagnostic> Diagnostics { get; set; } = new List<Diagnostic>();
        public ICollection<Intervention> Interventions { get; set; } = new List<Intervention>();
    }
}
