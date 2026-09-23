using DigitalAllianceTogo.Domain.Models.Catalogue;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persistence.Configurations.Catalogue
{
    public class AttributProduitConfiguration : IEntityTypeConfiguration<AttributProduit>
    {
        public void Configure(EntityTypeBuilder<AttributProduit> builder)
        {
            builder.ToTable("AttributsProduit");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Cle).IsRequired().HasMaxLength(100);
            builder.Property(a => a.Valeur).IsRequired().HasMaxLength(500);
        }
    }
}
