namespace DigitalAllianceTogo.Domain.Models.Security
{
    public class Role
    {
        public Guid Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public ICollection<UtilisateurRole> UtilisateurRoles { get; set; } = new List<UtilisateurRole>();
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
