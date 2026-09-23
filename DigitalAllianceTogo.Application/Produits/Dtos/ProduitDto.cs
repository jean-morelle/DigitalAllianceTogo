using DigitalAllianceTogo.Domain.Models.Catalogue;

namespace DigitalAllianceTogo.Application.Produits.Dtos
{
    /// <summary>Vue "liste" : légère, sans images/attributs (évite le sur-fetch).</summary>
    public class ProduitDto
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public decimal Prix { get; set; }
        public bool Actif { get; set; }
        public DateTime DateCreation { get; set; }
        public string CategorieNom { get; set; } = string.Empty;
        public string MarqueNom { get; set; } = string.Empty;
        public string? ImagePrincipaleUrl { get; set; }

        public static ProduitDto FromEntity(Produit produit)
        {
            return new ProduitDto
            {
                Id = produit.Id,
                Reference = produit.Reference,
                Nom = produit.Nom,
                Prix = produit.Prix,
                Actif = produit.Actif,
                DateCreation = produit.DateCreation,
                CategorieNom = produit.Categorie?.Nom ?? string.Empty,
                MarqueNom = produit.Marque?.Nom ?? string.Empty,
                ImagePrincipaleUrl = produit.Images?.FirstOrDefault(i => i.EstPrincipale)?.Url
            };
        }
    }
}
