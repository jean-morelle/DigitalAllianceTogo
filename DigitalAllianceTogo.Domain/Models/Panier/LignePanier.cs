namespace DigitalAllianceTogo.Domain.Models.Panier
{
    public class LignePanier
    {
        public Guid Id { get; set; }
        public int Quantite { get; set; }
        public decimal PrixUnitaire { get; set; }
        public DateTime DateAjout { get; set; } = DateTime.UtcNow;

        public Guid PanierId { get; set; }
        public Panier Panier { get; set; } = null!;

        public Guid ProduitId { get; set; }
        public Models.Catalogue.Produit Produit { get; set; } = null!;
    }
}
