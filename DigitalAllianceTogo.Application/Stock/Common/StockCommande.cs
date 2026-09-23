using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Stock;
using Microsoft.EntityFrameworkCore;
using CommandeEntity = DigitalAllianceTogo.Domain.Models.Commande.Commande;
using LivraisonEntity = DigitalAllianceTogo.Domain.Models.Livraison.Livraison;

namespace DigitalAllianceTogo.Application.Stock.Common
{
    /// <summary>
    /// Effets d'une commande sur le stock après sa réservation (§11-18). Tout se retrouve
    /// à partir des MouvementStock de la commande (CommandeId) et de la livraison (Reference) :
    ///
    ///   réservé pour la commande     = Réservations − Libérations − Sorties
    ///   en transit pour la livraison = Sorties − Retours (référence de la livraison)
    ///
    /// N'enregistre rien : l'appelant fait un seul SaveChanges (xmin sur StockProduit).
    /// </summary>
    internal static class StockCommande
    {
        /// <summary>
        /// Remise au livreur : la SEULE sortie de stock. Réservé − q, physique − q, en transit + q.
        /// </summary>
        public static async Task<List<object>> SortirAsync(IApplicationDbContext context, LivraisonEntity livraison, CancellationToken cancellationToken)
        {
            var reserves = await ReservesAsync(context, livraison.CommandeId, cancellationToken);
            if (reserves.Count == 0)
                throw new ConflictException("Aucun stock n'est réservé pour cette commande : rien à remettre au livreur.");

            var sorties = new List<object>();
            foreach (var (stock, quantite) in reserves)
            {
                stock.QuantiteReservee -= quantite;
                stock.QuantitePhysique -= quantite;
                stock.QuantiteEnTransit += quantite;

                context.MouvementsStock.Add(Mouvement(TypeMouvementStock.Sortie, quantite, "Remise au livreur", livraison.Reference, livraison.CommandeId, stock.Id));
                sorties.Add(new { stock.ProduitId, stock.EntrepotId, Quantite = quantite });
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
        /// Client absent : le colis revient au dépôt (en transit − q, physique + q)
        /// et reste réservé pour la relivraison.
        /// </summary>
        public static async Task RetournerPourRelivraisonAsync(IApplicationDbContext context, LivraisonEntity livraison, CancellationToken cancellationToken)
        {
            foreach (var (stock, quantite) in await EnTransitAsync(context, livraison, cancellationToken))
            {
                stock.QuantiteEnTransit -= quantite;
                stock.QuantitePhysique += quantite;
                stock.QuantiteReservee += quantite;
                context.MouvementsStock.Add(Mouvement(TypeMouvementStock.Retour, quantite, "Retour au dépôt : client absent", livraison.Reference, livraison.CommandeId, stock.Id));
                context.MouvementsStock.Add(Mouvement(TypeMouvementStock.Reservation, quantite, "Maintien de la réservation pour relivraison", livraison.Reference, livraison.CommandeId, stock.Id));
            }
        }

        /// <summary>Annulation avant préparation : la réservation est libérée (réservé − q).</summary>
        public static async Task<List<object>> LibererAsync(IApplicationDbContext context, CommandeEntity commande, CancellationToken cancellationToken)
        {
            var liberations = new List<object>();
            foreach (var (stock, quantite) in await ReservesAsync(context, commande.Id, cancellationToken))
            {
                stock.QuantiteReservee -= quantite;
                context.MouvementsStock.Add(Mouvement(TypeMouvementStock.Liberation, quantite, "Annulation de la commande", commande.Reference, commande.Id, stock.Id));
                liberations.Add(new { stock.ProduitId, stock.EntrepotId, Quantite = quantite });
            }
            return liberations;
        }

        /// <summary>
        /// Réception contrôlée des produits d'une commande annulée en préparation
        /// (produits réservés) ou refusée à la livraison (produits en transit) — §16, §18.
        /// Chaque unité est classée : intacte → vendable, endommagée → défectueuse.
        /// </summary>
        /// <param name="defectueux">Quantité endommagée par produit (le reste est intact).</param>
        /// <param name="livraisonRefusee">La livraison refusée, ou null pour une annulation en préparation.</param>
        public static async Task<List<object>> ReceptionnerRetourAsync(
            IApplicationDbContext context,
            CommandeEntity commande,
            LivraisonEntity? livraisonRefusee,
            IReadOnlyDictionary<Guid, int> defectueux,
            CancellationToken cancellationToken)
        {
            var aControler = livraisonRefusee is null
                ? await ReservesAsync(context, commande.Id, cancellationToken)
                : await EnTransitAsync(context, livraisonRefusee, cancellationToken);

            if (aControler.Count == 0)
                throw new ConflictException("Aucun produit de cette commande n'est à réceptionner.");

            foreach (var (produitId, quantite) in defectueux)
            {
                var total = aControler.Where(x => x.Stock.ProduitId == produitId).Sum(x => x.Quantite);
                if (quantite > total)
                    throw new ConflictException($"{quantite} produit(s) déclaré(s) défectueux, mais seulement {total} à contrôler pour ce produit.");
            }

            var resteDefectueux = defectueux.ToDictionary(d => d.Key, d => d.Value);
            var controle = new List<object>();
            foreach (var (stock, quantite) in aControler)
            {
                var abimes = Math.Min(quantite, resteDefectueux.GetValueOrDefault(stock.ProduitId));
                resteDefectueux[stock.ProduitId] = resteDefectueux.GetValueOrDefault(stock.ProduitId) - abimes;
                var intacts = quantite - abimes;

                if (livraisonRefusee is null)
                {
                    // Produits encore au dépôt : on libère la réservation, les abîmés sortent du vendable
                    stock.QuantiteReservee -= quantite;
                    context.MouvementsStock.Add(Mouvement(TypeMouvementStock.Liberation, quantite, "Annulation pendant la préparation", commande.Reference, commande.Id, stock.Id));
                    if (abimes > 0)
                    {
                        stock.QuantitePhysique -= abimes;
                        stock.QuantiteDefectueuse += abimes;
                        context.MouvementsStock.Add(Mouvement(TypeMouvementStock.Ajustement, abimes, "Contrôle après annulation : défectueux", commande.Reference, commande.Id, stock.Id));
                    }
                }
                else
                {
                    // Colis refusé : il revient du transit
                    stock.QuantiteEnTransit -= quantite;
                    stock.QuantitePhysique += intacts;
                    stock.QuantiteDefectueuse += abimes;
                    if (intacts > 0)
                        context.MouvementsStock.Add(Mouvement(TypeMouvementStock.Retour, intacts, "Retour après refus : intact", livraisonRefusee.Reference, commande.Id, stock.Id));
                    if (abimes > 0)
                        context.MouvementsStock.Add(Mouvement(TypeMouvementStock.Retour, abimes, "Retour après refus : défectueux", livraisonRefusee.Reference, commande.Id, stock.Id));
                }

                controle.Add(new { stock.ProduitId, stock.EntrepotId, Intacts = intacts, Defectueux = abimes });
            }
            return controle;
        }

        /// <summary>Vrai s'il reste du stock réservé pour la commande.</summary>
        public static async Task<bool> ADuStockReserveAsync(IApplicationDbContext context, Guid commandeId, CancellationToken cancellationToken) =>
            (await ReservesAsync(context, commandeId, cancellationToken)).Count > 0;

        private static async Task<List<(StockProduit Stock, int Quantite)>> ReservesAsync(
            IApplicationDbContext context, Guid commandeId, CancellationToken cancellationToken)
        {
            var parStock = await context.MouvementsStock
                .Where(m => m.CommandeId == commandeId)
                .GroupBy(m => m.StockProduitId)
                .Select(g => new
                {
                    StockProduitId = g.Key,
                    Quantite = g.Where(m => m.Type == TypeMouvementStock.Reservation).Sum(m => m.Quantite)
                             - g.Where(m => m.Type == TypeMouvementStock.Liberation || m.Type == TypeMouvementStock.Sortie).Sum(m => m.Quantite)
                })
                .Where(x => x.Quantite > 0)
                .ToListAsync(cancellationToken);

            var stocks = await ChargerStocksAsync(context, parStock.Select(x => x.StockProduitId), cancellationToken);
            return parStock.Select(x => (stocks[x.StockProduitId], x.Quantite)).ToList();
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

        private static MouvementStock Mouvement(TypeMouvementStock type, int quantite, string motif, string reference, Guid commandeId, Guid stockProduitId) => new()
        {
            Id = Guid.NewGuid(),
            Type = type,
            Quantite = quantite,
            DateMouvement = DateTime.UtcNow,
            Motif = motif,
            Reference = reference,
            CommandeId = commandeId,
            StockProduitId = stockProduitId
        };
    }
}
