using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Stock;
using Microsoft.EntityFrameworkCore;
using CommandeEntity = DigitalAllianceTogo.Domain.Models.Commande.Commande;

namespace DigitalAllianceTogo.Application.Stock.Common
{
    /// <summary>
    /// Réservation du stock d'une commande payée (§11).
    ///
    /// - Tout ou rien : si UN seul produit manque, rien n'est réservé et la commande
    ///   passe en EnAttenteDisponibilite (jamais de réservation partielle).
    /// - Une ligne peut être servie par plusieurs entrepôts (le plus fourni d'abord).
    /// - Chaque réservation laisse un MouvementStock rattaché à la commande.
    ///
    /// N'enregistre rien : l'appelant fait un seul SaveChanges (le jeton xmin de
    /// StockProduit empêche deux commandes de réserver les mêmes unités).
    /// </summary>
    internal static class ReservationStock
    {
        public static async Task<bool> TenterAsync(
            IApplicationDbContext context,
            IAuditService audit,
            CommandeEntity commande,
            CancellationToken cancellationToken)
        {
            var besoins = await context.LignesCommande
                .Where(l => l.VersionCommande.CommandeId == commande.Id
                            && l.VersionCommande.NumeroVersion == commande.VersionActive)
                .GroupBy(l => l.ProduitId)
                .Select(g => new { ProduitId = g.Key, Quantite = g.Sum(l => l.Quantite) })
                .ToListAsync(cancellationToken);

            // Après une modification de commande (§20), une partie peut déjà être réservée :
            // on ne réserve que ce qui manque (tout ou rien sur ce manque).
            var dejaReserve = await StockCommande.ReserveParProduitAsync(context, commande.Id, cancellationToken);
            besoins = besoins
                .Select(b => new { b.ProduitId, Quantite = b.Quantite - dejaReserve.GetValueOrDefault(b.ProduitId) })
                .Where(b => b.Quantite > 0)
                .ToList();

            var produitIds = besoins.Select(b => b.ProduitId).ToList();

            // Entités suivies : si une réservation précédente (même requête) a déjà
            // consommé du stock, EF renvoie les valeurs modifiées en mémoire.
            var stocks = await context.StocksProduit
                .Where(s => produitIds.Contains(s.ProduitId) && s.Entrepot.Actif)
                .ToListAsync(cancellationToken);

            var avant = commande.Statut.ToString();

            var manquants = besoins
                .Where(b => stocks.Where(s => s.ProduitId == b.ProduitId).Sum(s => s.QuantiteDisponible) < b.Quantite)
                .Select(b => b.ProduitId)
                .ToList();

            if (manquants.Count > 0)
            {
                if (commande.Statut != StatutCommande.EnAttenteDisponibilite)
                {
                    commande.Statut = StatutCommande.EnAttenteDisponibilite;
                    audit.Enregistrer("AttenteDisponibilite", "Commande", commande.Id,
                        new { Statut = avant }, new { Statut = commande.Statut.ToString(), ProduitsManquants = manquants });
                }
                return false;
            }

            var reservations = new List<object>();
            foreach (var besoin in besoins)
            {
                var reste = besoin.Quantite;
                foreach (var stock in stocks.Where(s => s.ProduitId == besoin.ProduitId && s.QuantiteDisponible > 0)
                                            .OrderByDescending(s => s.QuantiteDisponible))
                {
                    if (reste == 0) break;

                    var quantite = Math.Min(reste, stock.QuantiteDisponible);
                    stock.QuantiteReservee += quantite;
                    reste -= quantite;

                    context.MouvementsStock.Add(new MouvementStock
                    {
                        Id = Guid.NewGuid(),
                        Type = TypeMouvementStock.Reservation,
                        Quantite = quantite,
                        DateMouvement = DateTime.UtcNow,
                        Motif = "Réservation après paiement confirmé",
                        Reference = commande.Reference,
                        CommandeId = commande.Id,
                        StockProduitId = stock.Id
                    });
                    reservations.Add(new { besoin.ProduitId, stock.EntrepotId, Quantite = quantite });
                }
            }

            commande.Statut = StatutCommande.StockReserve;
            audit.Enregistrer("ReservationStock", "Commande", commande.Id,
                new { Statut = avant }, new { Statut = commande.Statut.ToString(), Reservations = reservations });
            return true;
        }
    }
}
