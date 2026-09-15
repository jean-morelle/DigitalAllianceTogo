namespace DigitalAllianceTogo.Application.Common.Exceptions
{
    /// <summary>
    /// Levée pour une ressource non trouvée.
    /// À mapper vers un 404 dans le middleware d'erreurs de l'API.
    /// </summary>
    public class NotFoundException : Exception
    {

        public NotFoundException(string name, object key)
            : base($"L'entité \"{name}\" avec la clé ({key}) est introuvable.")
        {
        }
    }
}
