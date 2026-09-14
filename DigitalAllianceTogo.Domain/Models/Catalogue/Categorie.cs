namespace DigitalAllianceTogo.Domain.Models.Catalogue
{
    public class Categorie
    {
        public Guid Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Actif { get; set; } = true;

        public ICollection<Produit> Produits { get; set; } = new List<Produit>();
    }
}
