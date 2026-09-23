using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Domain.Models.Security;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Clients.Common
{
    internal static class ClientHelper
    {
        public static bool EstPersonnel(ICurrentUserService currentUser) =>
            currentUser.EstDansRole(Roles.Admin) || currentUser.EstDansRole(Roles.Commercial);

        /// <summary>
        /// Charge une fiche client : le personnel accède à toutes, un client uniquement à la sienne.
        /// </summary>
        public static async Task<Client> ChargerAvecControleAccesAsync(
            IApplicationDbContext context, ICurrentUserService currentUser, Guid clientId, CancellationToken cancellationToken)
        {
            var client = await context.Clients
                .Include(c => c.Utilisateur)
                .FirstOrDefaultAsync(c => c.Id == clientId, cancellationToken)
                ?? throw new NotFoundException("Client", clientId);

            if (!EstPersonnel(currentUser) && client.UtilisateurId != currentUser.UtilisateurId)
                throw new ForbiddenAccessException();

            return client;
        }

        /// <summary>Fiche client du compte connecté (rôle Client).</summary>
        public static async Task<Guid> IdClientConnecteAsync(
            IApplicationDbContext context, ICurrentUserService currentUser, CancellationToken cancellationToken)
        {
            var utilisateurId = currentUser.UtilisateurId ?? throw new UnauthorizedException("Vous devez être connecté.");

            var clientId = await context.Clients
                .Where(c => c.UtilisateurId == utilisateurId)
                .Select(c => (Guid?)c.Id)
                .FirstOrDefaultAsync(cancellationToken);

            return clientId ?? throw new NotFoundException("Client", $"utilisateur {utilisateurId}");
        }

        public static string? NormaliserEmail(string? email) =>
            string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

        public static object Instantane(Client client) => new
        {
            client.CodeClient,
            Type = client.Type.ToString(),
            client.Nom,
            client.Prenom,
            client.RaisonSociale,
            client.Telephone,
            client.Email,
            Source = client.Source.ToString()
        };
    }
}
