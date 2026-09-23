using DigitalAllianceTogo.Domain.Models.Stock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persistence.Configurations.Stock
{
    public class MouvementStockConfiguration : IEntityTypeConfiguration<MouvementStock>
    {
        public void Configure(EntityTypeBuilder<MouvementStock> builder)
        {
            builder.ToTable("MouvementsStock");
            builder.HasKey(m => m.Id);

            // Enum stocké en texte (lisible en base, robuste si on réordonne les valeurs)
            builder.Property(m => m.Type)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(m => m.Motif).HasMaxLength(500);
            builder.Property(m => m.Reference).HasMaxLength(100);
        }
    }
}
