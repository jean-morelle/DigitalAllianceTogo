using DigitalAllianceTogo.Domain.Enum;

namespace DigitalAllianceTogo.Domain.Models.Stock
{
    public class MouvementStock
    {
        public Guid Id { get; set; }
        public TypeMouvementStock Type { get; set; }
        public int Quantite { get; set; }
        public DateTime DateMouvement { get; set; } = DateTime.UtcNow;
        public string Motif { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;

        public Guid StockProduitId { get; set; }
        public StockProduit StockProduit { get; set; } = null!;
    }
}
