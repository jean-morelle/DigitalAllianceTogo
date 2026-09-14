using DigitalAllianceTogo.Domain.Models.Commande;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persitence.Configurations.Commande
{
    public class LigneCommandeConfiguration : IEntityTypeConfiguration<LigneCommande>
    {
        public void Configure(EntityTypeBuilder<LigneCommande> builder)
        {
            builder.ToTable("LignesCommande");
            builder.HasKey(l => l.Id);

            builder.Property(l => l.Quantite).IsRequired();

            // LigneCommande (1) -- (0..*) TicketSAV : produit concerné
            builder.HasMany(l => l.TicketsSAV)
                .WithOne(t => t.LigneCommande)
                .HasForeignKey(t => t.LigneCommandeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
