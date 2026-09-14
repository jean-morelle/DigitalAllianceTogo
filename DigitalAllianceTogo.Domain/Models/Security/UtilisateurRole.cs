namespace DigitalAllianceTogo.Domain.Models.Security
{
    public class UtilisateurRole
    {
        public Guid Id { get; set; }
        public DateTime DateAffectation { get; set; } = DateTime.UtcNow;

        public Guid UtilisateurId { get; set; }
        public Utilisateur Utilisateur { get; set; } = null!;

        public Guid RoleId { get; set; }
        public Role Role { get; set; } = null!;
    }
}
