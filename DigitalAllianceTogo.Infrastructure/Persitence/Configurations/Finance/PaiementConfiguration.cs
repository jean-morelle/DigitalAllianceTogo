using DigitalAllianceTogo.Domain.Models.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persitence.Configurations.Finance
{
    public class PaiementConfiguration : IEntityTypeConfiguration<Paiement>
    {

        public void Configure(EntityTypeBuilder<Paiement> builder)
        {
            builder.ToTable("Paiements");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Reference).IsRequired().HasMaxLength(50);
            builder.HasIndex(p => p.Reference).IsUnique();

            builder.Property(p => p.Statut)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(p => p.Mode)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();
        }
    }
}
