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

        // Commande à l'origine du mouvement (réservation, libération, sortie...) :
        // permet de retrouver exactement le stock réservé pour une commande.
        // Ticket SAV à l'origine du mouvement (remplacement, retour de l'ancien produit)
        public Guid? TicketSAVId { get; set; }
        public Models.SAV.TicketSAV? TicketSAV { get; set; }

        public Guid? CommandeId { get; set; }
        public Models.Commande.Commande? Commande { get; set; }

        public Guid StockProduitId { get; set; }
        public StockProduit StockProduit { get; set; } = null!;
    }
}
