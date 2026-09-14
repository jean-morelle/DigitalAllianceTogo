namespace DigitalAllianceTogo.Domain.Models.Devis
{
    public class LigneDevis
    {
        public Guid Id { get; set; }
        public int Quantite { get; set; }
        public decimal PrixUnitaire { get; set; }
        public decimal Remise { get; set; }
        public decimal Total { get; set; }

        public Guid DevisId { get; set; }
        public Devis Devis { get; set; } = null!;

        public Guid ProduitId { get; set; }
        public Models.Catalogue.Produit Produit { get; set; } = null!;
    }
}
