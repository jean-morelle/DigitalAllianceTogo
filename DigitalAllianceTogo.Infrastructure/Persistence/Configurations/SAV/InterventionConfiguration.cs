using DigitalAllianceTogo.Domain.Models.SAV;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persistence.Configurations.SAV
{
    public class InterventionConfiguration : IEntityTypeConfiguration<Intervention>
    {
        public void Configure(EntityTypeBuilder<Intervention> builder)
        {
            builder.ToTable("Interventions");
            builder.HasKey(i => i.Id);

            builder.Property(i => i.Description).IsRequired().HasMaxLength(2000);
            builder.Property(i => i.Resultat).HasMaxLength(2000);
        }
    }
}
