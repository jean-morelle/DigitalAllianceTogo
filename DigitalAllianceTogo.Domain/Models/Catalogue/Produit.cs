namespace DigitalAllianceTogo.Domain.Models.Catalogue
{
    public class Produit
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Prix { get; set; }
        public bool Actif { get; set; } = true;
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        // Produit (0..*) -- (1) Categorie / Marque
        public Guid CategorieId { get; set; }
        public Categorie Categorie { get; set; } = null!;

        public Guid MarqueId { get; set; }
        public Marque Marque { get; set; } = null!;

        public ICollection<ImageProduit> Images { get; set; } = new List<ImageProduit>();
        public ICollection<AttributProduit> Attributs { get; set; } = new List<AttributProduit>();

        public ICollection<Models.Stock.StockProduit> Stocks { get; set; } = new List<Models.Stock.StockProduit>();
        public ICollection<Models.Devis.LigneDevis> LignesDevis { get; set; } = new List<Models.Devis.LigneDevis>();
        public ICollection<Models.Commande.LigneCommande> LignesCommande { get; set; } = new List<Models.Commande.LigneCommande>();
        public ICollection<Models.Panier.LignePanier> LignesPanier { get; set; } = new List<Models.Panier.LignePanier>();
    }
}
