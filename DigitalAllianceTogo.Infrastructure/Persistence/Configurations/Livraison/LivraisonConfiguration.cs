using DigitalAllianceTogo.Domain.Models.Livraison;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persistence.Configurations.Livraison
{
    public class LivraisonConfiguration : IEntityTypeConfiguration<Domain.Models.Livraison.Livraison>
    {
        public void Configure(EntityTypeBuilder<DigitalAllianceTogo.Domain.Models.Livraison.Livraison> builder)
        {
            builder.ToTable("Livraisons");
            builder.HasKey(l => l.Id);

            builder.Property(l => l.Reference).IsRequired().HasMaxLength(50);
            builder.HasIndex(l => l.Reference).IsUnique();
            builder.HasIndex(l => l.Statut);

            builder.Property(l => l.Type)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(l => l.Statut)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(l => l.Reserve).HasMaxLength(1000);
            builder.Property(l => l.MotifEchec).HasMaxLength(500);

            // Livraison (1) -- (0..1) PreuveLivraison
            builder.HasOne(l => l.Preuve)
                .WithOne(p => p.Livraison)
                .HasForeignKey<PreuveLivraison>(p => p.LivraisonId)
                .OnDelete(DeleteBehavior.Cascade);

            // Livraison (0..*) --> (1) Utilisateur : livreur, optionnel tant que non assigné
            builder.HasOne(l => l.Livreur)
                .WithMany(u => u.LivraisonsEnTantQueLivreur)
                .HasForeignKey(l => l.LivreurId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
