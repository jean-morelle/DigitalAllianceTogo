namespace DigitalAllianceTogo.Application.Devis.Common
{
    /// <summary>
    /// Ligne envoyée par le client HTTP. Volontairement SANS prix unitaire :
    /// le prix est toujours lu en base, jamais pris du navigateur.
    /// </summary>
    public record LigneDevisInput
    {
        public Guid ProduitId { get; init; }
        public int Quantite { get; init; }

        /// <summary>Remise en montant (FCFA) sur cette ligne.</summary>
        public decimal Remise { get; init; }
    }
}
