using DigitalAllianceTogo.Domain.Models.Catalogue;

namespace DigitalAllianceTogo.Application.Produits.Dtos
{
    /// <summary>Vue "détail" : tout le produit, avec ses images et attributs.</summary>
    public class ProduitDetailDto
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Prix { get; set; }
        public bool Actif { get; set; }
        public DateTime DateCreation { get; set; }

        public Guid CategorieId { get; set; }
        public string CategorieNom { get; set; } = string.Empty;

        public Guid MarqueId { get; set; }
        public string MarqueNom { get; set; } = string.Empty;

        public List<ImageProduitDto> Images { get; set; } = new();
        public List<AttributProduitDto> Attributs { get; set; } = new();

        /// <summary>Au moins une unité disponible (renseigné par la requête de détail).</summary>
        public bool EnStock { get; set; }

        public static ProduitDetailDto FromEntity(Produit produit)
        {
            return new ProduitDetailDto
            {
                Id = produit.Id,
                Reference = produit.Reference,
                Nom = produit.Nom,
                Description = produit.Description,
                Prix = produit.Prix,
                Actif = produit.Actif,
                DateCreation = produit.DateCreation,
                CategorieId = produit.CategorieId,
                CategorieNom = produit.Categorie?.Nom ?? string.Empty,
                MarqueId = produit.MarqueId,
                MarqueNom = produit.Marque?.Nom ?? string.Empty,
                Images = produit.Images
                    .OrderBy(i => i.Ordre)
                    .Select(i => new ImageProduitDto { Id = i.Id, Url = i.Url, Ordre = i.Ordre, EstPrincipale = i.EstPrincipale })
                    .ToList(),
                Attributs = produit.Attributs
                    .OrderBy(a => a.Ordre)
                    .Select(a => new AttributProduitDto { Id = a.Id, Cle = a.Cle, Valeur = a.Valeur, Ordre = a.Ordre })
                    .ToList()
            };
        }
    }

    public class ImageProduitDto
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public int Ordre { get; set; }
        public bool EstPrincipale { get; set; }
    }

    public class AttributProduitDto
    {
        public Guid Id { get; set; }
        public string Cle { get; set; } = string.Empty;
        public string Valeur { get; set; } = string.Empty;
        public int Ordre { get; set; }
    }
}
