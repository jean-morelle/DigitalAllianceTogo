using DigitalAllianceTogo.Domain.Models.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persistence.Configurations.Security
{
    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.ToTable("Permissions");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Code).IsRequired().HasMaxLength(100);
            builder.HasIndex(p => p.Code).IsUnique();
            builder.Property(p => p.Description).HasMaxLength(500);
        }
    }
}
