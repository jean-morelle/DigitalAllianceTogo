namespace DigitalAllianceTogo.Domain.Models.Catalogue
{
    public class ImageProduit
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public int Ordre { get; set; }
        public bool EstPrincipale { get; set; }

        public Guid ProduitId { get; set; }
        public Produit Produit { get; set; } = null!;
    }
}
