namespace DigitalAllianceTogo.Domain.Models.Livraison
{
    public class PreuveLivraison
    {
        public Guid Id { get; set; }
        public DateTime DatePreuve { get; set; } = DateTime.UtcNow;
        public string? PhotoUrl { get; set; }
        public string? SignatureUrl { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Commentaire { get; set; }

        public Guid LivraisonId { get; set; }
        public Livraison Livraison { get; set; } = null!;
    }
}
