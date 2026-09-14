using DigitalAllianceTogo.Domain.Models.Catalogue;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persitence.Configurations.Catalogue
{
    public class ImageProduitConfiguration : IEntityTypeConfiguration<ImageProduit>
    {
        public void Configure(EntityTypeBuilder<ImageProduit> builder)
        {
            builder.ToTable("ImagesProduit");
            builder.HasKey(i => i.Id);

            builder.Property(i => i.Url).IsRequired().HasMaxLength(1000);
        }
    }
}
