using DigitalAllianceTogo.Domain.Models.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persitence.Configurations.Finance
{
    public class AvoirConfiguration : IEntityTypeConfiguration<Avoir>
    {
        public void Configure(EntityTypeBuilder<Avoir> builder)
        {
            builder.ToTable("Avoirs");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Reference).IsRequired().HasMaxLength(50);
            builder.HasIndex(a => a.Reference).IsUnique();

            builder.Property(a => a.Statut)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();
        }
    }
}
