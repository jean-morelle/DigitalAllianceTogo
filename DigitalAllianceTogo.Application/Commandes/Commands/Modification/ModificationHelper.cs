using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Commande;
using Microsoft.EntityFrameworkCore;
using CommandeEntity = DigitalAllianceTogo.Domain.Models.Commande.Commande;

namespace DigitalAllianceTogo.Application.Commandes.Commands.Modification
{
    internal static class ModificationHelper
    {
        /// <summary>
        /// Commande payée, pas encore partie chez le livreur (§20). Avant paiement, il n'y a
        /// rien à versionner ; après remise au livreur, on passe par un refus ou un SAV.
        /// </summary>
        private static readonly StatutCommande[] StatutsModifiables =
        {
            StatutCommande.PaiementConfirme, StatutCommande.EnAttenteDisponibilite, StatutCommande.StockReserve,
            StatutCommande.PreparationEnCours, StatutCommande.PretePourLivraison
        };

        public static readonly StatutVersionCommande[] StatutsEnCours =
            { StatutVersionCommande.EnValidationAdmin, StatutVersionCommande.EnAttenteClient };

        public static async Task VerifierModifiableAsync(IApplicationDbContext context, CommandeEntity commande, CancellationToken cancellationToken)
        {
            if (!StatutsModifiables.Contains(commande.Statut))
                throw new ConflictException($"Une commande au statut {commande.Statut} ne peut pas être modifiée " +
                    "(il faut qu'elle soit payée et pas encore remise au livreur).");

            if (await context.Livraisons.AnyAsync(l => l.CommandeId == commande.Id && l.TicketSAVId == null
                    && (l.Statut == StatutLivraison.Planifiee || l.Statut == StatutLivraison.EnTransit), cancellationToken))
                throw new ConflictException("Une livraison est planifiée ou en cours : annulez-la avant de modifier la commande.");
        }

        /// <summary>La proposition en cours (en validation Admin ou en attente du client).</summary>
        public static async Task<VersionCommande> ChargerPropositionAsync(IApplicationDbContext context, Guid commandeId, CancellationToken cancellationToken) =>
            await context.VersionsCommande
                .Include(v => v.Lignes)
                .FirstOrDefaultAsync(v => v.CommandeId == commandeId && StatutsEnCours.Contains(v.Statut), cancellationToken)
            ?? throw new ConflictException("Aucune proposition de modification en cours pour cette commande.");

        public static object Instantane(VersionCommande version) => new
        {
            version.NumeroVersion,
            Statut = version.Statut.ToString(),
            version.SousTotal,
            version.Remise,
            version.Total,
            version.MotifModification,
            Lignes = version.Lignes.Select(l => new { l.ProduitId, l.Quantite, l.PrixUnitaire, l.Remise, l.Total })
        };
    }
}
