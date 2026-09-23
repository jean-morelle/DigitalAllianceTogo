namespace DigitalAllianceTogo.Application.Common.Interfaces
{
    /// <summary>
    /// Journalise une opération importante (QUI, QUAND, QUOI, AVANT, APRÈS).
    /// L'entrée est ajoutée au DbContext sans sauvegarder : elle est enregistrée
    /// dans la MÊME transaction que l'opération métier (pas d'action sans trace,
    /// pas de trace sans action).
    /// </summary>
    public interface IAuditService
    {
        void Enregistrer(string action, string entite, Guid entiteId, object? avant = null, object? apres = null);
    }
}
