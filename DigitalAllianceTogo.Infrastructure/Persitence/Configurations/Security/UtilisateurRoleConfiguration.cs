using DigitalAllianceTogo.Domain.Models.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Data.Entity.ModelConfiguration;

namespace DigitalAllianceTogo.Infrastructure.Persitence.Configurations.Security
{
    public class UtilisateurRoleConfiguration : EntityTypeConfiguration<UtilisateurRole>
    {
        public void Configure(EntityTypeBuilder<UtilisateurRole> builder)
        {
            builder.ToTable("UtilisateurRoles");
            builder.HasKey(ur => ur.Id);

            builder.HasOne(ur => ur.Utilisateur)
                .WithMany(u => u.UtilisateurRoles)
                .HasForeignKey(ur => ur.UtilisateurId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ur => ur.Role)
                .WithMany(r => r.UtilisateurRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Empêche d'assigner deux fois le même rôle au même utilisateur
            builder.HasIndex(ur => new { ur.UtilisateurId, ur.RoleId }).IsUnique();
        }
    }
}
