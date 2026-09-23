using DigitalAllianceTogo.Domain.Models.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persistence.Configurations.Finance
{
    public class PaiementConfiguration : IEntityTypeConfiguration<Paiement>
    {

        public void Configure(EntityTypeBuilder<Paiement> builder)
        {
            builder.ToTable("Paiements");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Reference).IsRequired().HasMaxLength(50);
            builder.HasIndex(p => p.Reference).IsUnique();

            builder.Property(p => p.Statut)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(p => p.Mode)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(p => p.ReferenceExterne).HasMaxLength(100);
            builder.Property(p => p.PreuveUrl).HasMaxLength(500);

            // Utilisateur ayant confirmé le paiement : on garde la trace même si le compte est supprimé
            builder.HasOne(p => p.ConfirmePar)
                .WithMany()
                .HasForeignKey(p => p.ConfirmeParId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
