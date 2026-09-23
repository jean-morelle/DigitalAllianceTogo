using DigitalAllianceTogo.Domain.Models.Panier;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persistence.Configurations.Panier
{
    public class LignePanierConfiguration : IEntityTypeConfiguration<LignePanier>
    {
        public void Configure(EntityTypeBuilder<LignePanier> builder)
        {
            builder.ToTable("LignesPanier");
            builder.HasKey(l => l.Id);

            builder.Property(l => l.Quantite).IsRequired();

            // Un produit n'apparaît qu'une seule fois par panier (sinon on incrémente la quantité)
            builder.HasIndex(l => new { l.PanierId, l.ProduitId }).IsUnique();
        }
    }
}
