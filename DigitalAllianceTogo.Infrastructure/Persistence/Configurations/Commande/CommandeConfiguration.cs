using DigitalAllianceTogo.Domain.Models.Commande;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persistence.Configurations.Commande
{
    public class CommandeConfiguration : IEntityTypeConfiguration<DigitalAllianceTogo.Domain.Models.Commande.Commande>
    {
        public void Configure(EntityTypeBuilder<DigitalAllianceTogo.Domain.Models.Commande.Commande> builder)
        {
            builder.ToTable("Commandes");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Reference).IsRequired().HasMaxLength(50);
            builder.HasIndex(c => c.Reference).IsUnique();

            builder.Property(c => c.Version).IsRowVersion();

            builder.Property(c => c.Statut)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            // Commande (0..1) <-- (0..*) Devis : origine, optionnelle
            builder.HasOne(c => c.DevisOrigine)
                .WithMany(d => d.CommandesOrigine)
                .HasForeignKey(c => c.DevisOrigineId)
                .OnDelete(DeleteBehavior.SetNull);

            // Commande (1) -- (1) AdresseLivraisonCommande : un-à-un obligatoire
            builder.HasOne(c => c.AdresseLivraison)
                .WithOne(a => a.Commande)
                .HasForeignKey<AdresseLivraisonCommande>(a => a.CommandeId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            // Commande (1) *-- (1..*) VersionCommande : composition, tout l'historique
            // des versions disparaît si la commande est supprimée
            builder.HasMany(c => c.Versions)
                .WithOne(v => v.Commande)
                .HasForeignKey(v => v.CommandeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Commande (1) -- (0..*) Paiement / Remboursement / Avoir / Livraison :
            // on garde toujours la trace financière et logistique
            builder.HasMany(c => c.Paiements)
                .WithOne(p => p.Commande)
                .HasForeignKey(p => p.CommandeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.Remboursements)
                .WithOne(r => r.Commande)
                .HasForeignKey(r => r.CommandeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.Avoirs)
                .WithOne(a => a.Commande)
                .HasForeignKey(a => a.CommandeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.Livraisons)
                .WithOne(l => l.Commande)
                .HasForeignKey(l => l.CommandeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
