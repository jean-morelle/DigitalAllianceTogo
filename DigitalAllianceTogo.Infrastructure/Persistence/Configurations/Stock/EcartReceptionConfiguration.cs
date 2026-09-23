using DigitalAllianceTogo.Domain.Models.Stock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persistence.Configurations.Stock
{
    public class EcartReceptionConfiguration : IEntityTypeConfiguration<EcartReception>
    {
        public void Configure(EntityTypeBuilder<EcartReception> builder)
        {
            builder.ToTable("EcartsReception");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Reference).IsRequired().HasMaxLength(100);
            builder.Property(e => e.MotifDecision).HasMaxLength(500);
            builder.Property(e => e.Statut)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();
            builder.Property(e => e.Version).IsRowVersion();

            // Calculé : jamais stocké
            builder.Ignore(e => e.Surplus);

            builder.HasIndex(e => e.Statut);

            builder.HasOne(e => e.Produit)
                .WithMany()
                .HasForeignKey(e => e.ProduitId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(e => e.Entrepot)
                .WithMany()
                .HasForeignKey(e => e.EntrepotId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
