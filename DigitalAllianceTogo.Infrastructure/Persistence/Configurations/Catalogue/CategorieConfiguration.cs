using DigitalAllianceTogo.Domain.Models.Catalogue;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persistence.Configurations.Catalogue
{
    public class CategorieConfiguration : IEntityTypeConfiguration<Categorie>
    {
        public void Configure(EntityTypeBuilder<Categorie> builder)
        {
            builder.ToTable("Categories");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Nom).IsRequired().HasMaxLength(150);
            builder.HasIndex(c => c.Nom).IsUnique();
            builder.Property(c => c.Description).HasMaxLength(1000);

            // Categorie (1) -- (0..*) Produit : on ne supprime jamais un catalogue produits
            // par erreur en supprimant une catégorie
            builder.HasMany(c => c.Produits)
                .WithOne(p => p.Categorie)
                .HasForeignKey(p => p.CategorieId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
