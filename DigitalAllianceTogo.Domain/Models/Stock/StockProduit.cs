namespace DigitalAllianceTogo.Domain.Models.Stock
{
    public class StockProduit
    {
        public Guid Id { get; set; }
        public int QuantitePhysique { get; set; }
        public int QuantiteReservee { get; set; }

        // Sorti de l'entrepôt (remis au livreur) mais pas encore livré.
        // Ne fait plus partie de QuantitePhysique.
        public int QuantiteEnTransit { get; set; }

        // Produits retournés/contrôlés endommagés : non vendables, hors QuantitePhysique.
        public int QuantiteDefectueuse { get; set; }

        public int SeuilAlerte { get; set; }

        // Jeton de concurrence optimiste (colonne système xmin de PostgreSQL)
        public uint Version { get; set; }

        // Propriété calculée : jamais stockée en base (voir config EF : Ignore)
        public int QuantiteDisponible => QuantitePhysique - QuantiteReservee;

        public Guid EntrepotId { get; set; }
        public Entrepot Entrepot { get; set; } = null!;

        public Guid ProduitId { get; set; }
        public Models.Catalogue.Produit Produit { get; set; } = null!;

        public ICollection<MouvementStock> Mouvements { get; set; } = new List<MouvementStock>();
    }
}
