namespace DigitalAllianceTogo.Domain.Models.Commande
{
    public class LigneCommande
    {
        public Guid Id { get; set; }
        public int Quantite { get; set; }
        public decimal PrixUnitaire { get; set; }
        public decimal Remise { get; set; }
        public decimal Total { get; set; }

        public Guid VersionCommandeId { get; set; }
        public VersionCommande VersionCommande { get; set; } = null!;

        public Guid ProduitId { get; set; }
        public Models.Catalogue.Produit Produit { get; set; } = null!;

        // LigneCommande (1) -- (0..*) TicketSAV : produit concerné
        public ICollection<Models.SAV.TicketSAV> TicketsSAV { get; set; } = new List<Models.SAV.TicketSAV>();
    }
}
