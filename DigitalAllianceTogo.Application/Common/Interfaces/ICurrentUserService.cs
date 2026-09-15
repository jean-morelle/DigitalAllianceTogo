namespace DigitalAllianceTogo.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        /// <summary>
        /// Donne accès à l'utilisateur actuellement authentifié (extrait du token JWT).
        /// Implémentation concrète dans l'API, basée sur HttpContext — l'Application
        /// n'a aucune dépendance à ASP.NET Core, elle utilise juste cette abstraction.
        /// </summary>
        Guid? UtilisateurId { get; }
        bool EstAuthentifie { get; }
    }
}
