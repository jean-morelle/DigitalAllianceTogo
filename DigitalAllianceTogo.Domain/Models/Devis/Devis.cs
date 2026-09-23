using DigitalAllianceTogo.Domain.Enum;

namespace DigitalAllianceTogo.Domain.Models.Devis
{
    public class Devis
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
        public DateTime DateValidite { get; set; }
        public StatutDevis Statut { get; set; } = StatutDevis.Brouillon;
        public decimal SousTotal { get; set; }
        public decimal Remise { get; set; }
        public decimal Total { get; set; }

        // Remise en % du sous-total : sert à appliquer le seuil Commercial / Administrateur.
        // Propriété calculée, jamais stockée en base.
        public decimal TauxRemise => SousTotal == 0 ? 0 : Math.Round(Remise / SousTotal * 100, 2);

        // Validation interne par l'entreprise (≠ acceptation par le client)
        public Guid? ValideParId { get; set; }
        public Models.Security.Utilisateur? ValidePar { get; set; }
        public DateTime? DateValidation { get; set; }

        // Commercial qui a créé le devis
        public Guid? CreeParId { get; set; }
        public Models.Security.Utilisateur? CreePar { get; set; }

        public Guid ClientId { get; set; }
        public Models.Security.Client Client { get; set; } = null!;

        // Devis (1) *-- (1..*) LigneDevis : composition
        public ICollection<LigneDevis> Lignes { get; set; } = new List<LigneDevis>();

        // Devis (0..1) --> (0..*) Commande : origine
        public ICollection<Models.Commande.Commande> CommandesOrigine { get; set; } = new List<Models.Commande.Commande>();
    }
}
