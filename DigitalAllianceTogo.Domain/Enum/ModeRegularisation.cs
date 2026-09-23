namespace DigitalAllianceTogo.Domain.Enum
{
    /// <summary>
    /// Comment l'entreprise rend l'argent d'une commande payée puis annulée ou refusée :
    /// remboursement (l'argent repart) ou avoir (crédit utilisable plus tard).
    /// </summary>
    public enum ModeRegularisation
    {
        Remboursement = 1,
        Avoir = 2
    }
}
