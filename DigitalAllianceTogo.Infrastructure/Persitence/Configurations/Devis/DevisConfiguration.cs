using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persitence.Configurations.Devis
{
    public class DevisConfiguration : IEntityTypeConfiguration<DigitalAllianceTogo.Domain.Models.Devis.Devis>
    {
        public void Configure(EntityTypeBuilder<DigitalAllianceTogo.Domain.Models.Devis.Devis> builder)
        {
            builder.ToTable("Devis");
            builder.HasKey(d => d.Id);

            builder.Property(d => d.Reference).IsRequired().HasMaxLength(50);
            builder.HasIndex(d => d.Reference).IsUnique();

            builder.Property(d => d.Statut)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            // Devis (1) *-- (1..*) LigneDevis : composition, supprimée avec le devis
            builder.HasMany(d => d.Lignes)
                .WithOne(l => l.Devis)
                .HasForeignKey(l => l.DevisId)
                .OnDelete(DeleteBehavior.Cascade);

            // Devis (0..1) --> (0..*) Commande : origine (voir CommandeConfiguration
            // pour la FK DevisOrigineId côté Commande)
        }
    }
}
