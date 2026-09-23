using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Enum;
using Microsoft.EntityFrameworkCore;
using CommandeEntity = DigitalAllianceTogo.Domain.Models.Commande.Commande;

namespace DigitalAllianceTogo.Application.Finance.Common
{
    /// <summary>
    /// Où en est l'argent d'une commande (hors SAV, qui a sa propre régularisation) :
    ///
    ///   payé net     = paiements confirmés − ce qui est rendu ou dû au client
    ///                  (remboursements, même échoués ; avoirs non annulés)
    ///   reste à payer = total de la version active − payé net
    ///
    /// Exemple §20 : v1 = 1 000 000 payée, v2 = 1 200 000 → reste 200 000 à payer.
    /// </summary>
    internal static class SoldeCommande
    {
        public static async Task<decimal> PayeNetAsync(IApplicationDbContext context, Guid commandeId, CancellationToken cancellationToken)
        {
            var paye = await context.Paiements
                .Where(p => p.CommandeId == commandeId && p.Statut == StatutPaiement.Confirme)
                .SumAsync(p => p.Montant, cancellationToken);
            var rembourse = await context.Remboursements
                .Where(r => r.CommandeId == commandeId && r.TicketSAVId == null)
                .SumAsync(r => r.Montant, cancellationToken);
            var avoirs = await context.Avoirs
                .Where(a => a.CommandeId == commandeId && a.TicketSAVId == null && a.Statut != StatutAvoir.Annule)
                .SumAsync(a => a.Montant, cancellationToken);

            return paye - rembourse - avoirs;
        }

        public static async Task<decimal> ResteAPayerAsync(IApplicationDbContext context, CommandeEntity commande, CancellationToken cancellationToken)
        {
            var total = await context.VersionsCommande
                .Where(v => v.CommandeId == commande.Id && v.NumeroVersion == commande.VersionActive)
                .Select(v => v.Total)
                .FirstAsync(cancellationToken);
            return total - await PayeNetAsync(context, commande.Id, cancellationToken);
        }

        /// <summary>Vrai si le client a déjà versé de l'argent qui ne lui a pas été rendu.</summary>
        public static async Task<bool> ADejaPayeAsync(IApplicationDbContext context, Guid commandeId, CancellationToken cancellationToken) =>
            await PayeNetAsync(context, commandeId, cancellationToken) > 0;
    }
}
