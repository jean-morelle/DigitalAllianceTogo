using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using PanierEntity = DigitalAllianceTogo.Domain.Models.Panier.Panier;

namespace DigitalAllianceTogo.Application.PanierClient
{
    internal static class PanierHelper
    {
        public const int QuantiteMaximaleParLigne = 100;

        /// <summary>Le panier actif du client connecté (créé au premier ajout).</summary>
        public static async Task<PanierEntity> ChargerOuCreerAsync(IApplicationDbContext context, Guid utilisateurId, CancellationToken cancellationToken)
        {
            var panier = await context.Paniers
                .Include(p => p.Lignes).ThenInclude(l => l.Produit)
                .FirstOrDefaultAsync(p => p.UtilisateurId == utilisateurId && p.Actif, cancellationToken);
            if (panier is not null)
                return panier;

            panier = new PanierEntity { Id = Guid.NewGuid(), UtilisateurId = utilisateurId, Actif = true };
            context.Paniers.Add(panier);
            return panier;
        }

        public static Guid UtilisateurConnecte(ICurrentUserService currentUser) =>
            currentUser.UtilisateurId ?? throw new UnauthorizedException("Connectez-vous pour utiliser le panier.");

        /// <summary>Fiche client du compte connecté : nécessaire pour un devis ou une commande.</summary>
        public static async Task<Guid> ClientConnecteAsync(IApplicationDbContext context, Guid utilisateurId, CancellationToken cancellationToken) =>
            await context.Clients.Where(c => c.UtilisateurId == utilisateurId).Select(c => (Guid?)c.Id).FirstOrDefaultAsync(cancellationToken)
            ?? throw new ForbiddenAccessException();
    }
}
