using DigitalAllianceTogo.Domain.Models.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persitence.Configurations.Security
{
    public class ClientConfiguration : IEntityTypeConfiguration<Client>
    {
        public void Configure(EntityTypeBuilder<Client> builder)
        {
            builder.ToTable("Clients");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.CodeClient).IsRequired().HasMaxLength(50);
            builder.HasIndex(c => c.CodeClient).IsUnique();

            // Client (0..1) -- (1) Utilisateur : relation un-à-un optionnelle
            builder.HasOne(c => c.Utilisateur)
                .WithOne(u => u.Client)
                .HasForeignKey<Client>(c => c.UtilisateurId)
                .OnDelete(DeleteBehavior.SetNull);

            // Client (1) -- (0..*) Adresse
            builder.HasMany(c => c.Adresses)
                .WithOne(a => a.Client)
                .HasForeignKey(a => a.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Client (1) -- (0..*) Devis / Commande / TicketSAV : on garde toujours l'historique
            builder.HasMany(c => c.Devis)
                .WithOne(d => d.Client)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.Commandes)
                .WithOne(cmd => cmd.Client)
                .HasForeignKey(cmd => cmd.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.TicketsSAV)
                .WithOne(t => t.Client)
                .HasForeignKey(t => t.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
