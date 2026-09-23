using DigitalAllianceTogo.Domain.Models.Livraison;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persistence.Configurations.Livraison
{
    public class PreuveLivraisonConfiguration : IEntityTypeConfiguration<PreuveLivraison>
    {
        public void Configure(EntityTypeBuilder<PreuveLivraison> builder)
        {
            builder.ToTable("PreuvesLivraison");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.PhotoUrl).HasMaxLength(1000);
            builder.Property(p => p.SignatureUrl).HasMaxLength(1000);
            builder.Property(p => p.Commentaire).HasMaxLength(1000);

            // Latitude/Longitude : precision GPS, override de la convention globale decimal(18,2)
            builder.Property(p => p.Latitude).HasPrecision(9, 6);
            builder.Property(p => p.Longitude).HasPrecision(9, 6);
        }
    }
}
