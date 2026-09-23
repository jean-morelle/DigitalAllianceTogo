namespace DigitalAllianceTogo.Domain.Models.Commande
{
    public class VersionCommande
    {
        public Guid Id { get; set; }
        public int NumeroVersion { get; set; }
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
        public string? MotifModification { get; set; }
        public decimal SousTotal { get; set; }
        public decimal Remise { get; set; }
        public decimal Total { get; set; }
        public bool Active { get; set; } = true;

        // Une fois Acceptée, le contenu (lignes, montants) ne change plus jamais :
        // seul le drapeau Active bascule quand une version suivante est acceptée.
        public Enum.StatutVersionCommande Statut { get; set; } = Enum.StatutVersionCommande.Acceptee;

        // Traçabilité de la proposition de modification (§20-21)
        public Guid? CreeParId { get; set; }
        public Guid? ValideParId { get; set; }
        public DateTime? DateReponse { get; set; }
        public string? MotifRefus { get; set; }

        // Baisse de prix : ce que le client a choisi pour le trop-perçu
        public Enum.ModeRegularisation? Regularisation { get; set; }

        public Guid CommandeId { get; set; }
        public Commande Commande { get; set; } = null!;

        // VersionCommande (1) *-- (1..*) LigneCommande : composition
        public ICollection<LigneCommande> Lignes { get; set; } = new List<LigneCommande>();

        // VersionCommande (1) <-- (*) Paiement / Remboursement / Avoir : concerne
        public ICollection<Models.Finance.Paiement> PaiementsConcernes { get; set; } = new List<Models.Finance.Paiement>();
        public ICollection<Models.Finance.Remboursement> RemboursementsConcernes { get; set; } = new List<Models.Finance.Remboursement>();
        public ICollection<Models.Finance.Avoir> AvoirsConcernes { get; set; } = new List<Models.Finance.Avoir>();
    }
}
