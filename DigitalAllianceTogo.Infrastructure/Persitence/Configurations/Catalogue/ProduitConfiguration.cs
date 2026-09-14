using DigitalAllianceTogo.Domain.Models.Catalogue;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persitence.Configurations.Catalogue
{
    public class ProduitConfiguration : IEntityTypeConfiguration<Produit>
    {
        public void Configure(EntityTypeBuilder<Produit> builder)
        {
            builder.ToTable("Produits");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Reference).IsRequired().HasMaxLength(50);
            builder.HasIndex(p => p.Reference).IsUnique();
            builder.Property(p => p.Nom).IsRequired().HasMaxLength(200);
            builder.Property(p => p.Description).HasMaxLength(4000);

            // Produit (1) -- (0..*) ImageProduit / AttributProduit :
            // ce sont des données "enfants" du produit, la cascade est logique ici
            builder.HasMany(p => p.Images)
                .WithOne(i => i.Produit)
                .HasForeignKey(i => i.ProduitId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.Attributs)
                .WithOne(a => a.Produit)
                .HasForeignKey(a => a.ProduitId)
                .OnDelete(DeleteBehavior.Cascade);

            // Produit (1) -- (0..*) StockProduit / LigneDevis / LigneCommande / LignePanier :
            // on ne supprime JAMAIS un produit en cascade avec son historique de ventes/stock
            builder.HasMany(p => p.Stocks)
                .WithOne(s => s.Produit)
                .HasForeignKey(s => s.ProduitId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(p => p.LignesDevis)
                .WithOne(l => l.Produit)
                .HasForeignKey(l => l.ProduitId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(p => p.LignesCommande)
                .WithOne(l => l.Produit)
                .HasForeignKey(l => l.ProduitId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(p => p.LignesPanier)
                .WithOne(l => l.Produit)
                .HasForeignKey(l => l.ProduitId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
