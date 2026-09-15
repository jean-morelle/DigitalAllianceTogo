using DigitalAllianceTogo.Domain.Models.Security;

namespace DigitalAllianceTogo.Application.Utilisateurs.Dtos
{
    public class UtilisateurDto
    {
        public Guid Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;
        public bool Actif { get; set; }
        public DateTime DateCreation { get; set; }
        public List<string> Roles { get; set; } = new();

        // Mapping manuel volontaire (pas d'AutoMapper) : reste explicite et
        // facile à déboguer. À reconsidérer si le nombre de DTOs explose.
        public static UtilisateurDto FromEntity(Utilisateur utilisateur)
        {
            return new UtilisateurDto
            {
                Id = utilisateur.Id,
                Nom = utilisateur.Nom,
                Prenom = utilisateur.Prenom,
                Email = utilisateur.Email,
                Telephone = utilisateur.Telephone,
                Actif = utilisateur.Actif,
                DateCreation = utilisateur.DateCreation,
                Roles = utilisateur.UtilisateurRoles.Select(ur => ur.Role.Nom).ToList()
            };
        }
    }
}

