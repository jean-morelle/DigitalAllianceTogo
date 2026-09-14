using DigitalAllianceTogo.Domain.Models.SAV;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persitence.Configurations.SAV
{
    public class DiagnosticConfiguration : IEntityTypeConfiguration<Diagnostic>
    {
        public void Configure(EntityTypeBuilder<Diagnostic> builder)
        {
            builder.ToTable("Diagnostics");
            builder.HasKey(d => d.Id);

            builder.Property(d => d.Conclusion).IsRequired().HasMaxLength(2000);
            builder.Property(d => d.Recommandation).HasMaxLength(2000);
        }
    }
}
