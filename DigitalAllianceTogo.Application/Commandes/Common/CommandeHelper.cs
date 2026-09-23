using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Devis.Common;
using DigitalAllianceTogo.Domain.Enum;
using Microsoft.EntityFrameworkCore;
using CommandeEntity = DigitalAllianceTogo.Domain.Models.Commande.Commande;

namespace DigitalAllianceTogo.Application.Commandes.Common
{
    internal static class CommandeHelper
    {
        /// <summary>
        /// Charge une commande : le personnel (Admin/Commercial) voit tout,
        /// un Client uniquement ses propres commandes.
        /// </summary>
        public static async Task<CommandeEntity> ChargerAvecControleAccesAsync(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            Guid commandeId,
            CancellationToken cancellationToken)
        {
            var commande = await context.Commandes
                .Include(c => c.Client)
                .Include(c => c.Paiements)
                .FirstOrDefaultAsync(c => c.Id == commandeId, cancellationToken)
                ?? throw new NotFoundException("Commande", commandeId);

            if (!DevisHelper.EstPersonnel(currentUser) && commande.Client.UtilisateurId != currentUser.UtilisateurId)
                throw new ForbiddenAccessException();

            return commande;
        }

        /// <summary>
        /// Moment à partir duquel court le délai de paiement :
        /// la création de la commande, ou le dernier rejet de paiement.
        /// Null si la commande n'attend pas de paiement de la part du client.
        /// </summary>
        public static DateTime? DebutDelaiPaiement(CommandeEntity commande) => commande.Statut switch
        {
            StatutCommande.CommandeCreee => commande.DateCreation,
            StatutCommande.PaiementEchoue => commande.Paiements
                .Where(p => p.Statut == StatutPaiement.Echoue)
                .Max(p => p.DateConfirmation ?? p.DatePaiement),
            _ => null
        };

        /// <summary>
        /// Annule une commande restée impayée au-delà du délai (§9).
        /// Aucun stock n'est réservé avant paiement : rien à libérer.
        /// Retourne vrai si la commande vient d'être annulée (l'appelant sauvegarde).
        /// </summary>
        public static bool AnnulerSiDelaiDepasse(CommandeEntity commande, int delaiHeures, DateTime maintenant)
        {
            var debut = DebutDelaiPaiement(commande);
            if (debut is null || debut.Value.AddHours(delaiHeures) > maintenant)
                return false;

            commande.Statut = StatutCommande.Annulee;
            return true;
        }
    }
}
