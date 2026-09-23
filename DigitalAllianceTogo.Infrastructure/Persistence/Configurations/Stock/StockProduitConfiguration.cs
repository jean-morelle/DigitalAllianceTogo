using DigitalAllianceTogo.Domain.Models.Stock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persistence.Configurations.Stock
{
    public class StockProduitConfiguration : IEntityTypeConfiguration<StockProduit>
    {
        public void Configure(EntityTypeBuilder<StockProduit> builder)
        {
            builder.ToTable("StocksProduit");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.QuantitePhysique).IsRequired();
            builder.Property(s => s.QuantiteReservee).IsRequired();
            builder.Property(s => s.QuantiteEnTransit).IsRequired();
            builder.Property(s => s.QuantiteDefectueuse).IsRequired();
            builder.Property(s => s.SeuilAlerte).IsRequired();

            // Propriété calculée en C# (QuantitePhysique - QuantiteReservee) :
            // ne doit JAMAIS être mappée en colonne, sinon EF essaiera de l'écrire.
            builder.Ignore(s => s.QuantiteDisponible);

            // Concurrence optimiste (xmin) : deux paiements confirmés en même temps
            // ne peuvent pas réserver les mêmes unités (pas de survente).
            builder.Property(s => s.Version).IsRowVersion();

            // Un même produit ne doit avoir qu'UNE seule ligne de stock par entrepôt
            builder.HasIndex(s => new { s.EntrepotId, s.ProduitId }).IsUnique();

            builder.HasMany(s => s.Mouvements)
                .WithOne(m => m.StockProduit)
                .HasForeignKey(m => m.StockProduitId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
