namespace DigitalAllianceTogo.Application.Common.Exceptions
{
    /// <summary>
    /// Levée pour un conflit métier (ex: email déjà utilisé, rôle déjà assigné).
    /// À mapper vers un 409 dans le middleware d'erreurs de l'API.
    /// </summary>
    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message)
        {
        }
    }
}
