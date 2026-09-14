using DigitalAllianceTogo.Domain.Models.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persitence.Configurations.Audit
{
    public class JournalAuditConfiguration : IEntityTypeConfiguration<JournalAudit>
    {
        public void Configure(EntityTypeBuilder<JournalAudit> builder)
        {
            builder.ToTable("JournauxAudit");
            builder.HasKey(j => j.Id);

            builder.Property(j => j.Action).IsRequired().HasMaxLength(200);
            builder.Property(j => j.Entite).IsRequired().HasMaxLength(200);
            builder.Property(j => j.AncienneValeur).HasColumnType("text");
            builder.Property(j => j.NouvelleValeur).HasColumnType("text");
            builder.Property(j => j.AdresseIP).HasMaxLength(45); // IPv6 max length

            // Index pour retrouver rapidement l'historique d'une entité donnée
            builder.HasIndex(j => new { j.Entite, j.EntiteId });
        }
    }
}
