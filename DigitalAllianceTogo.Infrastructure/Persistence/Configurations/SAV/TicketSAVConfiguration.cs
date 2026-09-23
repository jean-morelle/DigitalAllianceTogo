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

            builder.Property(t => t.Decision)
                .HasConversion<string>()
                .HasMaxLength(30);
            builder.Property(t => t.Resolution).HasMaxLength(1000);
            builder.Property(t => t.Version).IsRowVersion();

            // Technicien : on garde le ticket même si le compte est supprimé
            builder.HasOne(t => t.Technicien)
                .WithMany()
                .HasForeignKey(t => t.TechnicienId)
                .OnDelete(DeleteBehavior.SetNull);

            // Ce que le SAV a déclenché : historique conservé (Restrict)
            builder.HasMany<Domain.Models.Stock.MouvementStock>()
                .WithOne(m => m.TicketSAV)
                .HasForeignKey(m => m.TicketSAVId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasMany<Domain.Models.Livraison.Livraison>()
                .WithOne(l => l.TicketSAV)
                .HasForeignKey(l => l.TicketSAVId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasMany<Domain.Models.Finance.Remboursement>()
                .WithOne(r => r.TicketSAV)
                .HasForeignKey(r => r.TicketSAVId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasMany<Domain.Models.Finance.Avoir>()
                .WithOne(a => a.TicketSAV)
                .HasForeignKey(a => a.TicketSAVId)
                .OnDelete(DeleteBehavior.Restrict);

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
