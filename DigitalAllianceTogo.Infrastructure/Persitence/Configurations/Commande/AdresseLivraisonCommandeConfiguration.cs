using DigitalAllianceTogo.Domain.Models.Commande;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persitence.Configurations.Commande
{
    public class AdresseLivraisonCommandeConfiguration : IEntityTypeConfiguration<AdresseLivraisonCommande>
    {
        public void Configure(EntityTypeBuilder<AdresseLivraisonCommande> builder)
        {
            builder.ToTable("AdressesLivraisonCommande");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Ligne1).IsRequired().HasMaxLength(200);
            builder.Property(a => a.Ligne2).HasMaxLength(200);
            builder.Property(a => a.Ville).IsRequired().HasMaxLength(100);
            builder.Property(a => a.Pays).IsRequired().HasMaxLength(100);
            builder.Property(a => a.CodePostal).IsRequired().HasMaxLength(20);
            builder.Property(a => a.TelephoneContact).HasMaxLength(30);

            // FK unique déjà posée côté CommandeConfiguration (relation 1-1)
        }
    }
}
