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

        /// <summary>Adresse IP de l'appelant, pour le journal d'audit.</summary>
        string? AdresseIP { get; }

        /// <summary>Vrai si l'utilisateur possède ce rôle (voir Common.Security.Roles).</summary>
        bool EstDansRole(string role);
    }
}
