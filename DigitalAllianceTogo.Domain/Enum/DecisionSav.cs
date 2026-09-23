namespace DigitalAllianceTogo.Domain.Enum
{
    /// <summary>
    /// Choix du client, enregistré par le Commercial, quand le produit n'est pas réparable (§26).
    /// </summary>
    public enum DecisionSav
    {
        Remplacement = 1,
        Remboursement = 2,
        Avoir = 3
    }
}
