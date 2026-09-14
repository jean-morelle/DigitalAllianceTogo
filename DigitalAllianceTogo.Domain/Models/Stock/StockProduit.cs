namespace DigitalAllianceTogo.Domain.Models.Stock
{
    public class StockProduit
    {
        public Guid Id { get; set; }
        public int QuantitePhysique { get; set; }
        public int QuantiteReservee { get; set; }
        public int SeuilAlerte { get; set; }

        // Propriété calculée : jamais stockée en base (voir config EF : Ignore)
        public int QuantiteDisponible => QuantitePhysique - QuantiteReservee;

        public Guid EntrepotId { get; set; }
        public Entrepot Entrepot { get; set; } = null!;

        public Guid ProduitId { get; set; }
        public Models.Catalogue.Produit Produit { get; set; } = null!;

        public ICollection<MouvementStock> Mouvements { get; set; } = new List<MouvementStock>();
    }
}
