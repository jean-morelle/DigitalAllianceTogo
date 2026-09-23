using DigitalAllianceTogo.Domain.Models.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalAllianceTogo.Infrastructure.Persistence.Configurations.Security
{
    public class UtilisateurConfiguration : IEntityTypeConfiguration<Utilisateur>
    {
        public void Configure(EntityTypeBuilder<Utilisateur> builder)
        {
            builder.ToTable("Utilisateurs");
            builder.HasKey(u => u.Id);

            builder.Property(u => u.Nom).IsRequired().HasMaxLength(100);
            builder.Property(u => u.Prenom).IsRequired().HasMaxLength(100);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(255);
            builder.Property(u => u.Telephone).HasMaxLength(30);
            builder.Property(u => u.MotDePasseHash).IsRequired();

            // Email unique : contrainte métier forte, pas de doublon de compte
            builder.HasIndex(u => u.Email).IsUnique();

            // Utilisateur (1) -- (0..1) Client : configuré côté ClientConfiguration
            // (FK portée par Client.UtilisateurId)

            // Utilisateur (1) -- (0..*) Panier
            builder.HasMany(u => u.Paniers)
                .WithOne(p => p.Utilisateur)
                .HasForeignKey(p => p.UtilisateurId)
                .OnDelete(DeleteBehavior.Cascade);

            // Utilisateur (1) -- (0..*) JournalAudit : on garde toujours l'historique
            builder.HasMany(u => u.JournauxAudit)
                .WithOne(j => j.Utilisateur)
                .HasForeignKey(j => j.UtilisateurId)
                .OnDelete(DeleteBehavior.Restrict);

            // Utilisateur (0..*) -- (1) Livraison : livreur (optionnel)
            builder.HasMany(u => u.LivraisonsEnTantQueLivreur)
                .WithOne(l => l.Livreur)
                .HasForeignKey(l => l.LivreurId)
                .OnDelete(DeleteBehavior.SetNull);

            // Utilisateur (0..*) -- (1) Diagnostic / Intervention : technicien (obligatoire)
            builder.HasMany(u => u.DiagnosticsEnTantQueTechnicien)
                .WithOne(d => d.Technicien)
                .HasForeignKey(d => d.TechnicienId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.InterventionsEnTantQueTechnicien)
                .WithOne(i => i.Technicien)
                .HasForeignKey(i => i.TechnicienId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
