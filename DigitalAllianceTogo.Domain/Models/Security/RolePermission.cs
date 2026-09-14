namespace DigitalAllianceTogo.Domain.Models.Security
{
    public class RolePermission
    {
        public Guid Id { get; set; }

        public Guid RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public Guid PermissionId { get; set; }
        public Permission Permission { get; set; } = null!;
    }
}
