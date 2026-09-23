using DigitalAllianceTogo.Domain.Enum;

namespace DigitalAllianceTogo.Domain.Models.Stock
{
    /// <summary>
    /// Surplus fournisseur (§30) : plus d'unités livrées que commandées.
    /// Le surplus reste HORS stock jusqu'à la décision (intégration ou retour fournisseur).
    /// </summary>
    public class EcartReception
    {
        public Guid Id { get; set; }

        // Bon de livraison / facture fournisseur de la réception
        public string Reference { get; set; } = string.Empty;
        public int QuantiteCommandee { get; set; }
        public int QuantiteRecue { get; set; }
        public int Surplus => QuantiteRecue - QuantiteCommandee;

        public StatutEcartReception Statut { get; set; } = StatutEcartReception.EnAttenteDecision;
        public DateTime DateConstat { get; set; } = DateTime.UtcNow;
        public Guid ConstateParId { get; set; }

        public DateTime? DateDecision { get; set; }
        public Guid? DecideParId { get; set; }
        public string? MotifDecision { get; set; }

        // Jeton de concurrence optimiste (xmin) : une seule décision possible
        public uint Version { get; set; }

        public Guid ProduitId { get; set; }
        public Models.Catalogue.Produit Produit { get; set; } = null!;
        public Guid EntrepotId { get; set; }
        public Entrepot Entrepot { get; set; } = null!;
    }
}
