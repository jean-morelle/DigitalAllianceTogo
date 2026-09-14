namespace DigitalAllianceTogo.Domain.Models.Security
{
    public class Adresse
    {
        public Guid Id { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public string Ligne1 { get; set; } = string.Empty;
        public string? Ligne2 { get; set; }
        public string Ville { get; set; } = string.Empty;
        public string Pays { get; set; } = string.Empty;
        public string CodePostal { get; set; } = string.Empty;

        // Adresse (0..*) -- (1) Client
        public Guid ClientId { get; set; }
        public Client Client { get; set; } = null!;
    }
}
