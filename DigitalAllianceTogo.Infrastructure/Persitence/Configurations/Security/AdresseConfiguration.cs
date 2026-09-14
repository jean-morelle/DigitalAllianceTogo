using DigitalAllianceTogo.Domain.Models.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persitence.Configurations.Security
{
    public class AdresseConfiguration : IEntityTypeConfiguration<Adresse>
    {
        public void Configure(EntityTypeBuilder<Adresse> builder)
        {
            builder.ToTable("Adresses");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Libelle).IsRequired().HasMaxLength(100);
            builder.Property(a => a.Ligne1).IsRequired().HasMaxLength(200);
            builder.Property(a => a.Ligne2).HasMaxLength(200);
            builder.Property(a => a.Ville).IsRequired().HasMaxLength(100);
            builder.Property(a => a.Pays).IsRequired().HasMaxLength(100);
            builder.Property(a => a.CodePostal).IsRequired().HasMaxLength(20);

            // FK + cascade déjà configurés côté ClientConfiguration
        }
    }
}
