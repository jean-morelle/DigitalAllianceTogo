using DigitalAllianceTogo.Domain.Enum;

namespace DigitalAllianceTogo.Domain.Models.Security
{
    /// <summary>
    /// Fiche commerciale du client. Elle existe même SANS compte sur le site
    /// (client enregistré par un commercial depuis WhatsApp, en boutique...) :
    /// l'identité est donc portée ici, pas seulement par l'Utilisateur.
    /// </summary>
    public class Client
    {
        public Guid Id { get; set; }
        public string CodeClient { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        public TypeClient Type { get; set; } = TypeClient.Particulier;

        // Particulier : la personne. Entreprise : la personne de contact.
        public string Nom { get; set; } = string.Empty;
        public string? Prenom { get; set; }

        // Obligatoire pour une entreprise
        public string? RaisonSociale { get; set; }

        public string Telephone { get; set; } = string.Empty;
        public string? Email { get; set; }

        // Canal d'acquisition (WhatsApp, TikTok, Facebook, site...)
        public SourceClient Source { get; set; } = SourceClient.SiteWeb;

        // Client (0..1) -- (1) Utilisateur
        public Guid? UtilisateurId { get; set; }
        public Utilisateur? Utilisateur { get; set; }

        // Client (1) -- (0..*) Adresse
        public ICollection<Adresse> Adresses { get; set; } = new List<Adresse>();

        // Client (1) -- (0..*) Devis / Commande / TicketSAV
        public ICollection<Models.Devis.Devis> Devis { get; set; } = new List<Models.Devis.Devis>();
        public ICollection<Models.Commande.Commande> Commandes { get; set; } = new List<Models.Commande.Commande>();
        public ICollection<Models.SAV.TicketSAV> TicketsSAV { get; set; } = new List<Models.SAV.TicketSAV>();
    }
}
