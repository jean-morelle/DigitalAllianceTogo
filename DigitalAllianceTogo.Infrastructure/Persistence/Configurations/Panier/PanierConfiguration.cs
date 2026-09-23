using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persistence.Configurations.Panier
{
    public class PanierConfiguration : IEntityTypeConfiguration<Domain.Models.Panier.Panier>
    {
        public void Configure(EntityTypeBuilder<DigitalAllianceTogo.Domain.Models.Panier.Panier> builder)
        {
            builder.ToTable("Paniers");
            builder.HasKey(p => p.Id);

            // Panier (1) *-- (0..*) LignePanier
            builder.HasMany(p => p.Lignes)
                .WithOne(l => l.Panier)
                .HasForeignKey(l => l.PanierId)
                .OnDelete(DeleteBehavior.Cascade);

            // Règle métier "un seul panier Actif par utilisateur" : à faire respecter
            // dans la couche Application (pas exprimable proprement en contrainte SQL
            // portable ici sans index filtré propre à PostgreSQL).
        }
    }
}
