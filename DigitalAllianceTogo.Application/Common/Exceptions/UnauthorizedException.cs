namespace DigitalAllianceTogo.Application.Common.Exceptions
{
    /// <summary>
    /// Levée lorsque l'authentification échoue.
    /// À mapper vers un 401 dans le middleware d'erreurs de l'API.
    /// </summary>
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException()
            : base("Email ou mot de passe incorrect.")
        {
        }

        public UnauthorizedException(string message)
            : base(message)
        {
        }
    }
}
