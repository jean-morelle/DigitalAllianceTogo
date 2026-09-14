using DigitalAllianceTogo.Domain.Enum;

namespace DigitalAllianceTogo.Domain.Models.Commande
{
    public class Commande
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public StatutCommande Statut { get; set; } = StatutCommande.CommandeCreee;
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        // Numéro de la VersionCommande actuellement active (pointeur logique)
        public int VersionActive { get; set; } = 1;

        public Guid ClientId { get; set; }
        public Models.Security.Client Client { get; set; } = null!;

        // Commande (0..1) <-- (0..*) Devis : origine (nullable si commande créée sans devis)
        public Guid? DevisOrigineId { get; set; }
        public Models.Devis.Devis? DevisOrigine { get; set; }

        // Commande (1) -- (1) AdresseLivraisonCommande : snapshot obligatoire
        public AdresseLivraisonCommande AdresseLivraison { get; set; } = null!;

        // Commande (1) *-- (1..*) VersionCommande : composition, historique des versions
        public ICollection<VersionCommande> Versions { get; set; } = new List<VersionCommande>();

        // Commande (1) -- (0..*) Paiement / Remboursement / Avoir / Livraison
        public ICollection<Models.Finance.Paiement> Paiements { get; set; } = new List<Models.Finance.Paiement>();
        public ICollection<Models.Finance.Remboursement> Remboursements { get; set; } = new List<Models.Finance.Remboursement>();
        public ICollection<Models.Finance.Avoir> Avoirs { get; set; } = new List<Models.Finance.Avoir>();
        public ICollection<Models.Livraison.Livraison> Livraisons { get; set; } = new List<Models.Livraison.Livraison>();
    }
}
