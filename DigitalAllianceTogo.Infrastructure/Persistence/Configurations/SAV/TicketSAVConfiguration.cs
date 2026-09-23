using DigitalAllianceTogo.Domain.Models.SAV;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persistence.Configurations.SAV
{
    public class TicketSAVConfiguration : IEntityTypeConfiguration<TicketSAV>
    {
        public void Configure(EntityTypeBuilder<TicketSAV> builder)
        {
            builder.ToTable("TicketsSAV");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Reference).IsRequired().HasMaxLength(50);
            builder.HasIndex(t => t.Reference).IsUnique();
            builder.Property(t => t.Motif).IsRequired().HasMaxLength(1000);

            builder.Property(t => t.Statut)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            // TicketSAV (1) -- (0..*) Diagnostic / Intervention
            builder.HasMany(t => t.Diagnostics)
                .WithOne(d => d.TicketSAV)
                .HasForeignKey(d => d.TicketSAVId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(t => t.Interventions)
                .WithOne(i => i.TicketSAV)
                .HasForeignKey(i => i.TicketSAVId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
