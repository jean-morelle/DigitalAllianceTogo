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
        /// <param name="auteurId">
        /// Auteur explicite, pour une action faite sans être connecté
        /// (ex : inscription d'un client, l'auteur est le compte qui vient d'être créé).
        /// Par défaut : l'utilisateur authentifié.
        /// </param>
        void Enregistrer(string action, string entite, Guid entiteId, object? avant = null, object? apres = null, Guid? auteurId = null);

        /// <summary>Action automatique du système (tâche planifiée), sans utilisateur.</summary>
        void EnregistrerSysteme(string action, string entite, Guid entiteId, object? avant = null, object? apres = null);
    }
}
