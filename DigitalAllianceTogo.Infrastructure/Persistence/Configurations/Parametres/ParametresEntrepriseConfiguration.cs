using DigitalAllianceTogo.Domain.Models.Parametres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persistence.Configurations.Parametres
{
    public class ParametresEntrepriseConfiguration : IEntityTypeConfiguration<ParametresEntreprise>
    {
        // Id fixe : il n'existe qu'une seule ligne de paramètres
        public static readonly Guid IdParDefaut = new("5e1d7a3c-0b7e-4c1a-9a51-2f7c4d9e8a01");

        public void Configure(EntityTypeBuilder<ParametresEntreprise> builder)
        {
            builder.ToTable("ParametresEntreprise");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.SeuilRemiseCommercialPourcent).HasPrecision(5, 2);
            builder.Property(p => p.SeuilAugmentationModificationPourcent).HasPrecision(5, 2);

            // Valeurs par défaut du cahier des charges (remise commerciale max 10 %)
            builder.HasData(new ParametresEntreprise
            {
                Id = IdParDefaut,
                SeuilRemiseCommercialPourcent = 10m,
                SeuilAugmentationModificationPourcent = 10m,
                DelaiExpirationPaiementHeures = 48,
                DureeValiditeDevisJours = 15,
                DateModification = new DateTime(2026, 9, 23, 0, 0, 0, DateTimeKind.Utc)
            });
        }
    }
}
