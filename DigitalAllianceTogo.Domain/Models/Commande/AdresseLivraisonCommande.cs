namespace DigitalAllianceTogo.Domain.Models.Commande
{
    public class AdresseLivraisonCommande
    {
        public Guid Id { get; set; }
        public string Ligne1 { get; set; } = string.Empty;
        public string? Ligne2 { get; set; }
        public string Ville { get; set; } = string.Empty;
        public string Pays { get; set; } = string.Empty;
        public string CodePostal { get; set; } = string.Empty;
        public string TelephoneContact { get; set; } = string.Empty;

        public Guid CommandeId { get; set; }
        public Commande Commande { get; set; } = null!;
    }
}
