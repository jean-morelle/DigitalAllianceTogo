using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using Microsoft.EntityFrameworkCore;
using LivraisonEntity = DigitalAllianceTogo.Domain.Models.Livraison.Livraison;

namespace DigitalAllianceTogo.Application.Livraisons.Common
{
    internal static class LivraisonHelper
    {
        /// <summary>
        /// Admin et Gestionnaire de stock voient toutes les livraisons ;
        /// un Livreur uniquement celles qui lui sont assignées.
        /// </summary>
        public static bool VoitToutesLesLivraisons(ICurrentUserService currentUser) =>
            currentUser.EstDansRole(Roles.Admin) || currentUser.EstDansRole(Roles.GestionnaireStock);

        public static async Task<LivraisonEntity> ChargerAvecControleAccesAsync(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            Guid livraisonId,
            CancellationToken cancellationToken)
        {
            var livraison = await context.Livraisons
                .Include(l => l.Commande)
                .Include(l => l.TicketSAV)
                .FirstOrDefaultAsync(l => l.Id == livraisonId, cancellationToken)
                ?? throw new NotFoundException("Livraison", livraisonId);

            if (!VoitToutesLesLivraisons(currentUser) && livraison.LivreurId != currentUser.UtilisateurId)
                throw new ForbiddenAccessException();

            return livraison;
        }

        public static object Instantane(LivraisonEntity livraison) => new
        {
            Statut = livraison.Statut.ToString(),
            StatutCommande = livraison.Commande.Statut.ToString(),
            livraison.LivreurId,
            livraison.DatePlanifiee,
            livraison.MotifEchec,
            livraison.Reserve
        };
    }
}
