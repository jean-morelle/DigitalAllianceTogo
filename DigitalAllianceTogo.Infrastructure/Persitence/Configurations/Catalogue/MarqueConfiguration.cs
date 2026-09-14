using DigitalAllianceTogo.Domain.Models.Catalogue;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persitence.Configurations.Catalogue
{
    public class MarqueConfiguration : IEntityTypeConfiguration<Marque>
    {
        public void Configure(EntityTypeBuilder<Marque> builder)
        {
            builder.ToTable("Marques");
            builder.HasKey(m => m.Id);

            builder.Property(m => m.Nom).IsRequired().HasMaxLength(150);
            builder.HasIndex(m => m.Nom).IsUnique();
            builder.Property(m => m.Description).HasMaxLength(1000);

            builder.HasMany(m => m.Produits)
                .WithOne(p => p.Marque)
                .HasForeignKey(p => p.MarqueId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
