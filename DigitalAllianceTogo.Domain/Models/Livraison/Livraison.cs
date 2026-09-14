using DigitalAllianceTogo.Domain.Enum;

namespace DigitalAllianceTogo.Domain.Models.Livraison
{
    public class Livraison
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public TypeLivraison Type { get; set; }
        public StatutLivraison Statut { get; set; } = StatutLivraison.Planifiee;
        public DateTime DatePlanifiee { get; set; }
        public DateTime? DatePriseEnCharge { get; set; }
        public DateTime? DateLivraison { get; set; }

        public Guid CommandeId { get; set; }
        public Models.Commande.Commande Commande { get; set; } = null!;

        // Livraison (1) -- (0..1) PreuveLivraison
        public PreuveLivraison? Preuve { get; set; }

        // Livraison (0..*) --> (1) Utilisateur : livreur
        public Guid? LivreurId { get; set; }
        public Models.Security.Utilisateur? Livreur { get; set; }
    }
}
