namespace DigitalAllianceTogo.Domain.Models.Security
{
    public class Client
    {
        public Guid Id { get; set; }
        public string CodeClient { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

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
