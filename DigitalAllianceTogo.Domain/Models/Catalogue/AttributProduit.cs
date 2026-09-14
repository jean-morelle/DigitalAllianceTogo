namespace DigitalAllianceTogo.Domain.Models.Catalogue
{
    public class AttributProduit
    {
        public Guid Id { get; set; }
        public string Cle { get; set; } = string.Empty;
        public string Valeur { get; set; } = string.Empty;
        public int Ordre { get; set; }

        public Guid ProduitId { get; set; }
        public Produit Produit { get; set; } = null!;
    }
}
