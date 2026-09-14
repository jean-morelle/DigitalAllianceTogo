namespace DigitalAllianceTogo.Domain.Models.Stock
{
    public class Entrepot
    {
        public Guid Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Adresse { get; set; } = string.Empty;
        public bool Actif { get; set; } = true;

        public ICollection<StockProduit> Stocks { get; set; } = new List<StockProduit>();
    }
}
