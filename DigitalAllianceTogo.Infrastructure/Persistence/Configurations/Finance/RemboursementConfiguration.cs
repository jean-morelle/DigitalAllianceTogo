using DigitalAllianceTogo.Domain.Models.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persistence.Configurations.Finance
{
    public class RemboursementConfiguration : IEntityTypeConfiguration<Remboursement>
    {
        public void Configure(EntityTypeBuilder<Remboursement> builder)
        {
            builder.ToTable("Remboursements");
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Reference).IsRequired().HasMaxLength(50);
            builder.HasIndex(r => r.Reference).IsUnique();

            builder.Property(r => r.Motif).IsRequired().HasMaxLength(500);
            builder.Property(r => r.ReferenceTransaction).HasMaxLength(100);
            builder.Property(r => r.MotifEchec).HasMaxLength(500);

            builder.Property(r => r.Statut)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();
        }
    }
}
