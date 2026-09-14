namespace DigitalAllianceTogo.Domain.Models.Security
{
    public class Utilisateur
    {
        public Guid Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;
        public string MotDePasseHash { get; set; } = string.Empty;
        public bool Actif { get; set; } = true;
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        // Utilisateur (1) -- (0..1) Client
        public Client? Client { get; set; }

        // Utilisateur (1) -- (0..*) Panier / JournalAudit
        public ICollection<Models.Panier.Panier> Paniers { get; set; } = new List<Models.Panier.Panier>();
        public ICollection<Models.Audit.JournalAudit> JournauxAudit { get; set; } = new List<Models.Audit.JournalAudit>();

        // Utilisateur (*) -- (*) Role via UtilisateurRole
        public ICollection<UtilisateurRole> UtilisateurRoles { get; set; } = new List<UtilisateurRole>();

        // Utilisateur (1) -- (0..*) Livraison : livreur
        public ICollection<Models.Livraison.Livraison> LivraisonsEnTantQueLivreur { get; set; } = new List<Models.Livraison.Livraison>();

        // Utilisateur (1) -- (0..*) Diagnostic / Intervention : technicien
        public ICollection<Models.SAV.Diagnostic> DiagnosticsEnTantQueTechnicien { get; set; } = new List<Models.SAV.Diagnostic>();
        public ICollection<Models.SAV.Intervention> InterventionsEnTantQueTechnicien { get; set; } = new List<Models.SAV.Intervention>();
    }
}
