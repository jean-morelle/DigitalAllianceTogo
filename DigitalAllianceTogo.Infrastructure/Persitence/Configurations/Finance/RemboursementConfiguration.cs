using DigitalAllianceTogo.Domain.Models.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persitence.Configurations.Finance
{
    public class RemboursementConfiguration : IEntityTypeConfiguration<Remboursement>
    {
        public void Configure(EntityTypeBuilder<Remboursement> builder)
        {
            builder.ToTable("Remboursements");
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Reference).IsRequired().HasMaxLength(50);
            builder.HasIndex(r => r.Reference).IsUnique();

            builder.Property(r => r.Statut)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();
        }
    }
}
