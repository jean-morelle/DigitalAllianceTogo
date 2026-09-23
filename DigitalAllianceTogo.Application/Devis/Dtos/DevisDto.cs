namespace DigitalAllianceTogo.Application.Devis.Dtos
{
    public class DevisDto
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }
        public DateTime DateValidite { get; set; }
        public decimal SousTotal { get; set; }
        public decimal Remise { get; set; }
        public decimal Total { get; set; }
        public bool ValideParEntreprise { get; set; }
        public Guid ClientId { get; set; }
        public string CodeClient { get; set; } = string.Empty;
    }

    public class DevisDetailDto : DevisDto
    {
        public decimal TauxRemise { get; set; }
        public Guid? ValideParId { get; set; }
        public DateTime? DateValidation { get; set; }
        public Guid? CreeParId { get; set; }
        public string? CommentaireClient { get; set; }
        public string? CommentaireInterne { get; set; }
        public Guid? CommandeId { get; set; }
        public List<LigneDevisDto> Lignes { get; set; } = new();
    }

    public class LigneDevisDto
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
}
