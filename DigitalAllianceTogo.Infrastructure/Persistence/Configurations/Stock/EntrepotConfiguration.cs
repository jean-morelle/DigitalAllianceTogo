using DigitalAllianceTogo.Domain.Models.Stock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persistence.Configurations.Stock
{
    public class EntrepotConfiguration : IEntityTypeConfiguration<Entrepot>
    {
        public void Configure(EntityTypeBuilder<Entrepot> builder)
        {
            builder.ToTable("Entrepots");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Nom).IsRequired().HasMaxLength(150);
            builder.Property(e => e.Adresse).IsRequired().HasMaxLength(500);

            builder.HasMany(e => e.Stocks)
                .WithOne(s => s.Entrepot)
                .HasForeignKey(s => s.EntrepotId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
