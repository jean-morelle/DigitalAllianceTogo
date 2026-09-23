namespace DigitalAllianceTogo.Application.Common.Exceptions
{
    /// <summary>
    /// Levée quand l'utilisateur est authentifié mais n'a pas le droit d'agir
    /// sur cette ressource (ex : un client qui consulte le devis d'un autre client).
    /// À mapper vers un 403 dans le middleware d'erreurs de l'API.
    /// </summary>
    public class ForbiddenAccessException : Exception
    {
        public ForbiddenAccessException()
            : base("Vous n'avez pas accès à cette ressource.")
        {
        }

        public ForbiddenAccessException(string message)
            : base(message)
        {
        }
    }
}
