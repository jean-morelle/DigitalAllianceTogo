using DigitalAllianceTogo.Domain.Models.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persistence.Configurations.Finance
{
    public class AvoirConfiguration : IEntityTypeConfiguration<Avoir>
    {
        public void Configure(EntityTypeBuilder<Avoir> builder)
        {
            builder.ToTable("Avoirs");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Reference).IsRequired().HasMaxLength(50);
            builder.HasIndex(a => a.Reference).IsUnique();

            builder.Property(a => a.Motif).IsRequired().HasMaxLength(500);

            // Calculé : jamais stocké
            builder.Ignore(a => a.MontantRestant);
            builder.Property(a => a.Version).IsRowVersion();

            // Paiements réglés avec cet avoir : l'historique est conservé
            builder.HasMany<Paiement>()
                .WithOne(p => p.Avoir)
                .HasForeignKey(p => p.AvoirId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(a => a.Statut)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();
        }
    }
}
