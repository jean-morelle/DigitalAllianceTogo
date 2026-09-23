using DigitalAllianceTogo.Domain.Models.Devis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persistence.Configurations.Devis
{
    public class LigneDevisConfiguration : IEntityTypeConfiguration<LigneDevis>
    {
        public void Configure(EntityTypeBuilder<LigneDevis> builder)
        {
            builder.ToTable("LignesDevis");
            builder.HasKey(l => l.Id);

            builder.Property(l => l.Quantite).IsRequired();
        }
    }
}
