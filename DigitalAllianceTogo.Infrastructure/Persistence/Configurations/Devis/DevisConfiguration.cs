using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persistence.Configurations.Devis
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

            // Calculée à partir de Remise / SousTotal
            builder.Ignore(d => d.TauxRemise);

            builder.Property(d => d.CommentaireClient).HasMaxLength(1000);
            builder.Property(d => d.CommentaireInterne).HasMaxLength(1000);

            // Mappé sur la colonne système xmin : aucune colonne créée, PostgreSQL la gère
            builder.Property(d => d.Version).IsRowVersion();

            builder.HasOne(d => d.ValidePar)
                .WithMany()
                .HasForeignKey(d => d.ValideParId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(d => d.CreePar)
                .WithMany()
                .HasForeignKey(d => d.CreeParId)
                .OnDelete(DeleteBehavior.Restrict);

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
