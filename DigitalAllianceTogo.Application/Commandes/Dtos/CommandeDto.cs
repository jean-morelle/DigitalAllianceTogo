namespace DigitalAllianceTogo.Application.Commandes.Dtos
{
    public class CommandeDto
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }
        public int VersionActive { get; set; }
        public decimal Total { get; set; }
        public Guid ClientId { get; set; }
        public string CodeClient { get; set; } = string.Empty;
    }

    public class CommandeDetailDto : CommandeDto
    {
        public decimal SousTotal { get; set; }
        public decimal Remise { get; set; }
        public Guid? DevisOrigineId { get; set; }
        public AdresseLivraisonCommandeDto AdresseLivraison { get; set; } = new();
        public List<LigneCommandeDto> Lignes { get; set; } = new();
        public List<PaiementDto> Paiements { get; set; } = new();
        public List<Finance.Queries.RemboursementDto> Remboursements { get; set; } = new();
        public List<Finance.Queries.AvoirDto> Avoirs { get; set; } = new();

        /// <summary>Suivi des livraisons (initiale, relivraison, remplacement SAV).</summary>
        public List<SuiviLivraisonDto> Livraisons { get; set; } = new();

        /// <summary>Total de la version active − payé net (négatif : trop-perçu à rendre).</summary>
        public decimal ResteAPayer { get; set; }

        /// <summary>Historique complet des versions (§20) : jamais écrasées, propositions comprises.</summary>
        public List<VersionCommandeDto> Versions { get; set; } = new();

        /// <summary>Date limite pour payer (ou repayer), si la commande attend le client.</summary>
        public DateTime? DateLimitePaiement { get; set; }
    }

    public record SuiviLivraisonDto(string Reference, string Type, string Statut, DateTime DatePlanifiee, DateTime? DateLivraison, string? MotifEchec);

    public class VersionCommandeDto
    {
        public Guid Id { get; set; }
        public int NumeroVersion { get; set; }
        public string Statut { get; set; } = string.Empty;
        public bool Active { get; set; }
        public DateTime DateCreation { get; set; }
        public string? MotifModification { get; set; }
        public string? MotifRefus { get; set; }
        public DateTime? DateReponse { get; set; }
        public decimal SousTotal { get; set; }
        public decimal Remise { get; set; }
        public decimal Total { get; set; }
        public List<LigneCommandeDto> Lignes { get; set; } = new();
    }

    public class AdresseLivraisonCommandeDto
    {
        public string Ligne1 { get; set; } = string.Empty;
        public string? Ligne2 { get; set; }
        public string Ville { get; set; } = string.Empty;
        public string Pays { get; set; } = string.Empty;
        public string CodePostal { get; set; } = string.Empty;
        public string TelephoneContact { get; set; } = string.Empty;
    }

    public class LigneCommandeDto
    {
        public Guid Id { get; set; }
        public Guid ProduitId { get; set; }
        public string ProduitReference { get; set; } = string.Empty;
        public string ProduitNom { get; set; } = string.Empty;
        public int Quantite { get; set; }
        public decimal PrixUnitaire { get; set; }
        public decimal Remise { get; set; }
        public decimal Total { get; set; }
    }

    public class PaiementDto
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public decimal Montant { get; set; }
        public string Statut { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        public DateTime DatePaiement { get; set; }
        public string? ReferenceExterne { get; set; }
        public string? PreuveUrl { get; set; }
        public DateTime? DateConfirmation { get; set; }
        public string? MotifRejet { get; set; }
        public int NumeroVersion { get; set; }
        public Guid CommandeId { get; set; }
        public string CommandeReference { get; set; } = string.Empty;
    }
}
