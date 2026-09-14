using DigitalAllianceTogo.Domain.Models.Commande;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persitence.Configurations.Commande
{
    public class VersionCommandeConfiguration : IEntityTypeConfiguration<VersionCommande>
    {
        public void Configure(EntityTypeBuilder<VersionCommande> builder)
        {
            builder.ToTable("VersionsCommande");
            builder.HasKey(v => v.Id);

            builder.Property(v => v.MotifModification).HasMaxLength(1000);

            // Un même numéro de version ne peut apparaître deux fois pour la même commande
            builder.HasIndex(v => new { v.CommandeId, v.NumeroVersion }).IsUnique();

            // VersionCommande (1) *-- (1..*) LigneCommande : composition
            builder.HasMany(v => v.Lignes)
                .WithOne(l => l.VersionCommande)
                .HasForeignKey(l => l.VersionCommandeId)
                .OnDelete(DeleteBehavior.Cascade);

            // VersionCommande (1) <-- (*) Paiement / Remboursement / Avoir : concerne
            builder.HasMany(v => v.PaiementsConcernes)
                .WithOne(p => p.VersionCommande)
                .HasForeignKey(p => p.VersionCommandeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(v => v.RemboursementsConcernes)
                .WithOne(r => r.VersionCommande)
                .HasForeignKey(r => r.VersionCommandeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(v => v.AvoirsConcernes)
                .WithOne(a => a.VersionCommande)
                .HasForeignKey(a => a.VersionCommandeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
