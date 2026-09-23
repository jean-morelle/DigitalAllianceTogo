using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Stock;
using Microsoft.EntityFrameworkCore;
using LivraisonEntity = DigitalAllianceTogo.Domain.Models.Livraison.Livraison;

namespace DigitalAllianceTogo.Application.Stock.Common
{
    /// <summary>
    /// Effets d'une livraison sur le stock (§11-12). Tout se retrouve à partir des
    /// MouvementStock de la commande (CommandeId) et de la livraison (Reference) :
    ///
    ///   réservé pour la commande = Réservations − Libérations − Sorties
    ///   en transit pour la livraison = Sorties − Retours (référence de la livraison)
    ///
    /// N'enregistre rien : l'appelant fait un seul SaveChanges (xmin sur StockProduit).
    /// </summary>
    internal static class LivraisonStock
    {
        /// <summary>
        /// Remise au livreur : la SEULE sortie de stock. Réservé − q, physique − q, en transit + q.
        /// </summary>
        public static async Task<List<object>> SortirAsync(IApplicationDbContext context, LivraisonEntity livraison, CancellationToken cancellationToken)
        {
            var mouvements = await context.MouvementsStock
                .Where(m => m.CommandeId == livraison.CommandeId)
                .ToListAsync(cancellationToken);

            var reserves = mouvements
                .GroupBy(m => m.StockProduitId)
                .Select(g => new
                {
                    StockProduitId = g.Key,
                    Quantite = g.Where(m => m.Type == TypeMouvementStock.Reservation).Sum(m => m.Quantite)
                             - g.Where(m => m.Type is TypeMouvementStock.Liberation or TypeMouvementStock.Sortie).Sum(m => m.Quantite)
                })
                .Where(r => r.Quantite > 0)
                .ToList();

            if (reserves.Count == 0)
                throw new ConflictException("Aucun stock n'est réservé pour cette commande : rien à remettre au livreur.");

            var stocks = await ChargerStocksAsync(context, reserves.Select(r => r.StockProduitId), cancellationToken);
            var sorties = new List<object>();
            foreach (var reserve in reserves)
            {
                var stock = stocks[reserve.StockProduitId];
                stock.QuantiteReservee -= reserve.Quantite;
                stock.QuantitePhysique -= reserve.Quantite;
                stock.QuantiteEnTransit += reserve.Quantite;

                context.MouvementsStock.Add(Mouvement(TypeMouvementStock.Sortie, reserve.Quantite, "Remise au livreur", livraison, stock.Id));
                sorties.Add(new { stock.ProduitId, stock.EntrepotId, reserve.Quantite });
            }
            return sorties;
        }

        /// <summary>Livraison réussie : la marchandise quitte définitivement l'entreprise (en transit − q).</summary>
        public static async Task LivrerAsync(IApplicationDbContext context, LivraisonEntity livraison, CancellationToken cancellationToken)
        {
            foreach (var (stock, quantite) in await EnTransitAsync(context, livraison, cancellationToken))
                stock.QuantiteEnTransit -= quantite;
        }

        /// <summary>
        /// Échec : la marchandise revient au dépôt (en transit − q, physique + q).
        /// Client absent : elle reste réservée pour la relivraison. Refus : elle redevient vendable.
        /// </summary>
        public static async Task RetournerAsync(IApplicationDbContext context, LivraisonEntity livraison, bool garderReservation, CancellationToken cancellationToken)
        {
            foreach (var (stock, quantite) in await EnTransitAsync(context, livraison, cancellationToken))
            {
                stock.QuantiteEnTransit -= quantite;
                stock.QuantitePhysique += quantite;
                context.MouvementsStock.Add(Mouvement(TypeMouvementStock.Retour, quantite, "Retour au dépôt après échec de livraison", livraison, stock.Id));

                if (garderReservation)
                {
                    stock.QuantiteReservee += quantite;
                    context.MouvementsStock.Add(Mouvement(TypeMouvementStock.Reservation, quantite, "Maintien de la réservation pour relivraison", livraison, stock.Id));
                }
            }
        }

        private static async Task<List<(StockProduit Stock, int Quantite)>> EnTransitAsync(
            IApplicationDbContext context, LivraisonEntity livraison, CancellationToken cancellationToken)
        {
            var parStock = await context.MouvementsStock
                .Where(m => m.CommandeId == livraison.CommandeId && m.Reference == livraison.Reference
                            && (m.Type == TypeMouvementStock.Sortie || m.Type == TypeMouvementStock.Retour))
                .GroupBy(m => m.StockProduitId)
                .Select(g => new
                {
                    StockProduitId = g.Key,
                    Quantite = g.Where(m => m.Type == TypeMouvementStock.Sortie).Sum(m => m.Quantite)
                             - g.Where(m => m.Type == TypeMouvementStock.Retour).Sum(m => m.Quantite)
                })
                .Where(x => x.Quantite > 0)
                .ToListAsync(cancellationToken);

            var stocks = await ChargerStocksAsync(context, parStock.Select(x => x.StockProduitId), cancellationToken);
            return parStock.Select(x => (stocks[x.StockProduitId], x.Quantite)).ToList();
        }

        private static Task<Dictionary<Guid, StockProduit>> ChargerStocksAsync(
            IApplicationDbContext context, IEnumerable<Guid> ids, CancellationToken cancellationToken)
        {
            var liste = ids.ToList();
            return context.StocksProduit.Where(s => liste.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken);
        }

        private static MouvementStock Mouvement(TypeMouvementStock type, int quantite, string motif, LivraisonEntity livraison, Guid stockProduitId) => new()
        {
            Id = Guid.NewGuid(),
            Type = type,
            Quantite = quantite,
            DateMouvement = DateTime.UtcNow,
            Motif = motif,
            Reference = livraison.Reference,
            CommandeId = livraison.CommandeId,
            StockProduitId = stockProduitId
        };
    }
}
