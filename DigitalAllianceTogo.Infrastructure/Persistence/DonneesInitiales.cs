using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Domain.Models.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DigitalAllianceTogo.Infrastructure.Persistence
{
    /// <summary>
    /// Données indispensables au fonctionnement, créées au démarrage de l'API.
    /// Idempotent : ne crée que ce qui manque, ne modifie ni ne supprime rien d'existant.
    /// </summary>
    public static class DonneesInitiales
    {
        private static readonly (string Nom, string Description)[] RolesMetier =
        {
            (Roles.Admin, "Administrateur : utilisateurs, droits, validations financières et remises exceptionnelles"),
            (Roles.Commercial, "Commercial : clients, devis, commandes, remises dans la limite du seuil"),
            (Roles.GestionnaireStock, "Gestionnaire de stock : entrées, sorties, réservations, préparation, retours"),
            (Roles.Technicien, "Technicien : diagnostic et réparation SAV (aucune décision financière)"),
            (Roles.Livreur, "Livreur : missions de livraison, preuves, retours de colis"),
            (Roles.Client, "Client : devis, commandes, paiements, suivi, SAV"),
            (Roles.Catalogue, "Gestion du catalogue produits")
        };

        public static async Task InitialiserAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DonneesInitiales));

            await CreerRolesManquantsAsync(context, logger, cancellationToken);
            await CreerAdministrateurInitialAsync(context, scope.ServiceProvider, logger, cancellationToken);
        }

        private static async Task CreerRolesManquantsAsync(ApplicationDbContext context, ILogger logger, CancellationToken cancellationToken)
        {
            var existants = await context.Roles.Select(r => r.Nom).ToListAsync(cancellationToken);

            foreach (var (nom, description) in RolesMetier.Where(r => !existants.Contains(r.Nom)))
            {
                context.Roles.Add(new Role { Id = Guid.NewGuid(), Nom = nom, Description = description });
                logger.LogInformation("Rôle créé : {Role}", nom);
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Sans administrateur, personne ne peut attribuer de rôles (UtilisateursController est réservé à l'Admin).
        /// S'il n'en existe aucun, on en crée un à partir de la section "AdminInitial"
        /// (à mettre dans les user-secrets, JAMAIS dans appsettings.json).
        /// </summary>
        private static async Task CreerAdministrateurInitialAsync(
            ApplicationDbContext context, IServiceProvider services, ILogger logger, CancellationToken cancellationToken)
        {
            var adminExiste = await context.UtilisateurRoles.AnyAsync(ur => ur.Role.Nom == Roles.Admin, cancellationToken);
            if (adminExiste)
                return;

            var config = services.GetRequiredService<IConfiguration>().GetSection("AdminInitial");
            var email = config["Email"]?.Trim().ToLowerInvariant();
            var motDePasse = config["MotDePasse"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(motDePasse))
            {
                logger.LogWarning(
                    "Aucun administrateur n'existe. Définissez AdminInitial:Email et AdminInitial:MotDePasse " +
                    "dans les user-secrets puis redémarrez l'API.");
                return;
            }

            var utilisateur = await context.Utilisateurs.FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);
            if (utilisateur is null)
            {
                var hasher = services.GetRequiredService<IPasswordHasher>();
                utilisateur = new Utilisateur
                {
                    Id = Guid.NewGuid(),
                    Nom = config["Nom"] ?? "Administrateur",
                    Prenom = config["Prenom"] ?? "Principal",
                    Email = email,
                    MotDePasseHash = hasher.Hash(motDePasse),
                    Actif = true,
                    DateCreation = DateTime.UtcNow
                };
                context.Utilisateurs.Add(utilisateur);
            }

            var roleAdmin = await context.Roles.FirstAsync(r => r.Nom == Roles.Admin, cancellationToken);
            context.UtilisateurRoles.Add(new UtilisateurRole
            {
                Id = Guid.NewGuid(),
                UtilisateurId = utilisateur.Id,
                RoleId = roleAdmin.Id,
                DateAffectation = DateTime.UtcNow
            });

            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Administrateur initial configuré : {Email}", email);
        }
    }
}
