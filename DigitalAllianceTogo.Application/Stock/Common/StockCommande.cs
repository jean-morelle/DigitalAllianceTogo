using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.SAV;
using DigitalAllianceTogo.Domain.Models.Stock;
using Microsoft.EntityFrameworkCore;
using CommandeEntity = DigitalAllianceTogo.Domain.Models.Commande.Commande;
using LivraisonEntity = DigitalAllianceTogo.Domain.Models.Livraison.Livraison;

namespace DigitalAllianceTogo.Application.Stock.Common
{
    /// <summary>
    /// Effets sur le stock d'une commande (§11-18) ou d'un remplacement SAV (§27), après
    /// réservation. Le « porteur » de la réservation est soit la commande, soit le ticket SAV
    /// (jamais les deux : un SAV ne rouvre pas la commande). Tout se retrouve à partir des
    /// MouvementStock du porteur et de la livraison (Reference) :
    ///
    ///   réservé pour le porteur      = Réservations − Libérations − Sorties
    ///   en transit pour la livraison = Sorties − Retours (référence de la livraison)
    ///
    /// N'enregistre rien : l'appelant fait un seul SaveChanges (xmin sur StockProduit).
    /// </summary>
    internal static class StockCommande
    {
        private readonly record struct Porteur(Guid? CommandeId, Guid? TicketSAVId);

        private static Porteur DeCommande(Guid commandeId) => new(commandeId, null);
        private static Porteur DuTicket(Guid ticketId) => new(null, ticketId);
        private static Porteur DeLivraison(LivraisonEntity livraison) =>
            livraison.TicketSAVId is Guid ticketId ? DuTicket(ticketId) : DeCommande(livraison.CommandeId);

        /// <summary>
        /// Remise au livreur : la SEULE sortie de stock. Réservé − q, physique − q, en transit + q.
        /// </summary>
        public static async Task<List<object>> SortirAsync(IApplicationDbContext context, LivraisonEntity livraison, CancellationToken cancellationToken)
        {
            var porteur = DeLivraison(livraison);
            var reserves = await ReservesAsync(context, porteur, cancellationToken);
            if (reserves.Count == 0)
                throw new ConflictException("Aucun stock n'est réservé pour cette livraison : rien à remettre au livreur.");

            var sorties = new List<object>();
            foreach (var (stock, quantite) in reserves)
            {
                stock.QuantiteReservee -= quantite;
                stock.QuantitePhysique -= quantite;
                stock.QuantiteEnTransit += quantite;

                context.MouvementsStock.Add(Mouvement(TypeMouvementStock.Sortie, quantite, "Remise au livreur", livraison.Reference, porteur, stock.Id));
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
            var porteur = DeLivraison(livraison);
            foreach (var (stock, quantite) in await EnTransitAsync(context, livraison, cancellationToken))
            {
                stock.QuantiteEnTransit -= quantite;
                stock.QuantitePhysique += quantite;
                stock.QuantiteReservee += quantite;
                context.MouvementsStock.Add(Mouvement(TypeMouvementStock.Retour, quantite, "Retour au dépôt : client absent", livraison.Reference, porteur, stock.Id));
                context.MouvementsStock.Add(Mouvement(TypeMouvementStock.Reservation, quantite, "Maintien de la réservation pour relivraison", livraison.Reference, porteur, stock.Id));
            }
        }

        /// <summary>Annulation avant préparation : la réservation de la commande est libérée (réservé − q).</summary>
        public static Task<List<object>> LibererAsync(IApplicationDbContext context, CommandeEntity commande, CancellationToken cancellationToken) =>
            LibererAsync(context, DeCommande(commande.Id), commande.Reference, "Annulation de la commande", cancellationToken);

        /// <summary>Changement de décision SAV : le produit de remplacement réservé redevient disponible.</summary>
        public static Task<List<object>> LibererAsync(IApplicationDbContext context, TicketSAV ticket, CancellationToken cancellationToken) =>
            LibererAsync(context, DuTicket(ticket.Id), ticket.Reference, "Remplacement SAV abandonné", cancellationToken);

        /// <summary>
        /// Réserve le produit de remplacement d'un ticket SAV : tout ou rien, plusieurs
        /// entrepôts possibles (le plus fourni d'abord). Faux si le stock est insuffisant.
        /// </summary>
        public static async Task<bool> ReserverPourSavAsync(IApplicationDbContext context, TicketSAV ticket, Guid produitId, CancellationToken cancellationToken)
        {
            var porteur = DuTicket(ticket.Id);
            var dejaReserve = (await ReservesAsync(context, porteur, cancellationToken)).Sum(r => r.Quantite);
            var besoin = ticket.Quantite - dejaReserve;
            if (besoin <= 0)
                return true;

            var stocks = await context.StocksProduit
                .Where(s => s.ProduitId == produitId && s.Entrepot.Actif)
                .ToListAsync(cancellationToken);
            if (stocks.Sum(s => s.QuantiteDisponible) < besoin)
                return false;

            foreach (var stock in stocks.Where(s => s.QuantiteDisponible > 0).OrderByDescending(s => s.QuantiteDisponible))
            {
                if (besoin == 0) break;
                var quantite = Math.Min(besoin, stock.QuantiteDisponible);
                stock.QuantiteReservee += quantite;
                besoin -= quantite;
                context.MouvementsStock.Add(Mouvement(TypeMouvementStock.Reservation, quantite, "Produit de remplacement SAV", ticket.Reference, porteur, stock.Id));
            }
            return true;
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
            var porteur = DeCommande(commande.Id);
            var aControler = livraisonRefusee is null
                ? await ReservesAsync(context, porteur, cancellationToken)
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
                    context.MouvementsStock.Add(Mouvement(TypeMouvementStock.Liberation, quantite, "Annulation pendant la préparation", commande.Reference, porteur, stock.Id));
                    if (abimes > 0)
                    {
                        stock.QuantitePhysique -= abimes;
                        stock.QuantiteDefectueuse += abimes;
                        context.MouvementsStock.Add(Mouvement(TypeMouvementStock.Ajustement, abimes, "Contrôle après annulation : défectueux", commande.Reference, porteur, stock.Id));
                    }
                }
                else
                {
                    // Colis refusé : il revient du transit
                    stock.QuantiteEnTransit -= quantite;
                    stock.QuantitePhysique += intacts;
                    stock.QuantiteDefectueuse += abimes;
                    if (intacts > 0)
                        context.MouvementsStock.Add(Mouvement(TypeMouvementStock.Retour, intacts, "Retour après refus : intact", livraisonRefusee.Reference, porteur, stock.Id));
                    if (abimes > 0)
                        context.MouvementsStock.Add(Mouvement(TypeMouvementStock.Retour, abimes, "Retour après refus : défectueux", livraisonRefusee.Reference, porteur, stock.Id));
                }

                controle.Add(new { stock.ProduitId, stock.EntrepotId, Intacts = intacts, Defectueux = abimes });
            }
            return controle;
        }

        /// <summary>
        /// Ancien produit récupéré chez le client dans le cadre d'un SAV (§27) : il entre dans
        /// l'entrepôt choisi, réutilisable (physique + q) ou défectueux (défectueux + q).
        /// </summary>
        public static async Task<object> ReceptionnerAncienProduitAsync(
            IApplicationDbContext context, TicketSAV ticket, Guid produitId, Guid entrepotId, bool defectueux, CancellationToken cancellationToken)
        {
            var stock = await context.StocksProduit.FirstOrDefaultAsync(s => s.ProduitId == produitId && s.EntrepotId == entrepotId, cancellationToken);
            if (stock is null)
            {
                stock = new StockProduit { Id = Guid.NewGuid(), ProduitId = produitId, EntrepotId = entrepotId };
                context.StocksProduit.Add(stock);
            }

            if (defectueux)
                stock.QuantiteDefectueuse += ticket.Quantite;
            else
                stock.QuantitePhysique += ticket.Quantite;

            context.MouvementsStock.Add(Mouvement(TypeMouvementStock.Retour, ticket.Quantite,
                defectueux ? "Ancien produit SAV : défectueux" : "Ancien produit SAV : réutilisable",
                ticket.Reference, DuTicket(ticket.Id), stock.Id));

            return new { produitId, entrepotId, ticket.Quantite, Etat = defectueux ? "Defectueux" : "Reutilisable" };
        }

        /// <summary>Vrai s'il reste du stock réservé pour la commande.</summary>
        public static async Task<bool> ADuStockReserveAsync(IApplicationDbContext context, Guid commandeId, CancellationToken cancellationToken) =>
            (await ReservesAsync(context, DeCommande(commandeId), cancellationToken)).Count > 0;

        /// <summary>Unités déjà réservées pour la commande, par produit.</summary>
        public static async Task<Dictionary<Guid, int>> ReserveParProduitAsync(IApplicationDbContext context, Guid commandeId, CancellationToken cancellationToken) =>
            (await ReservesAsync(context, DeCommande(commandeId), cancellationToken))
                .GroupBy(r => r.Stock.ProduitId)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.Quantite));

        /// <summary>
        /// Modification de commande (§20) : libère ce qui est réservé au-delà des besoins de la
        /// nouvelle version (produit retiré ou quantité réduite). Le reste de la réservation est gardé.
        /// </summary>
        public static async Task<List<object>> LibererExcedentAsync(
            IApplicationDbContext context, CommandeEntity commande, IReadOnlyDictionary<Guid, int> besoins, CancellationToken cancellationToken)
        {
            var porteur = DeCommande(commande.Id);
            var reserves = await ReservesAsync(context, porteur, cancellationToken);
            var liberations = new List<object>();

            foreach (var parProduit in reserves.GroupBy(r => r.Stock.ProduitId))
            {
                var excedent = parProduit.Sum(r => r.Quantite) - besoins.GetValueOrDefault(parProduit.Key);
                foreach (var (stock, quantite) in parProduit)
                {
                    if (excedent <= 0) break;
                    var aLiberer = Math.Min(excedent, quantite);
                    stock.QuantiteReservee -= aLiberer;
                    excedent -= aLiberer;
                    context.MouvementsStock.Add(Mouvement(TypeMouvementStock.Liberation, aLiberer, "Modification de la commande", commande.Reference, porteur, stock.Id));
                    liberations.Add(new { stock.ProduitId, stock.EntrepotId, Quantite = aLiberer });
                }
            }
            return liberations;
        }

        /// <summary>Quantité réservée pour le remplacement d'un ticket SAV.</summary>
        public static async Task<int> QuantiteReserveeSavAsync(IApplicationDbContext context, Guid ticketId, CancellationToken cancellationToken) =>
            (await ReservesAsync(context, DuTicket(ticketId), cancellationToken)).Sum(r => r.Quantite);

        private static async Task<List<object>> LibererAsync(
            IApplicationDbContext context, Porteur porteur, string reference, string motif, CancellationToken cancellationToken)
        {
            var liberations = new List<object>();
            foreach (var (stock, quantite) in await ReservesAsync(context, porteur, cancellationToken))
            {
                stock.QuantiteReservee -= quantite;
                context.MouvementsStock.Add(Mouvement(TypeMouvementStock.Liberation, quantite, motif, reference, porteur, stock.Id));
                liberations.Add(new { stock.ProduitId, stock.EntrepotId, Quantite = quantite });
            }
            return liberations;
        }

        private static async Task<List<(StockProduit Stock, int Quantite)>> ReservesAsync(
            IApplicationDbContext context, Porteur porteur, CancellationToken cancellationToken)
        {
            var mouvements = porteur.TicketSAVId is Guid ticketId
                ? context.MouvementsStock.Where(m => m.TicketSAVId == ticketId)
                : context.MouvementsStock.Where(m => m.CommandeId == porteur.CommandeId && m.TicketSAVId == null);

            var parStock = await mouvements
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
            // La référence de livraison est unique : elle suffit à isoler ses sorties et retours
            var parStock = await context.MouvementsStock
                .Where(m => m.Reference == livraison.Reference
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

        private static MouvementStock Mouvement(TypeMouvementStock type, int quantite, string motif, string reference, Porteur porteur, Guid stockProduitId) => new()
        {
            Id = Guid.NewGuid(),
            Type = type,
            Quantite = quantite,
            DateMouvement = DateTime.UtcNow,
            Motif = motif,
            Reference = reference,
            CommandeId = porteur.CommandeId,
            TicketSAVId = porteur.TicketSAVId,
            StockProduitId = stockProduitId
        };
    }
}
