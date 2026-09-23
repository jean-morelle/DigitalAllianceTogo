namespace DigitalAllianceTogo.Domain.Models.Audit
{
    public class JournalAudit
    {
        public Guid Id { get; set; }
        public DateTime DateAction { get; set; } = DateTime.UtcNow;
        public string Action { get; set; } = string.Empty;
        public string Entite { get; set; } = string.Empty;
        public Guid EntiteId { get; set; }
        public string? AncienneValeur { get; set; }
        public string? NouvelleValeur { get; set; }
        public string? AdresseIP { get; set; }

        // Null = action du SYSTÈME (ex : expiration automatique d'une commande impayée)
        public Guid? UtilisateurId { get; set; }
        public Models.Security.Utilisateur? Utilisateur { get; set; }

    }
}
