using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Stock;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Stock.Common
{
    /// <summary>
    /// Entrée de marchandise vendable dans un entrepôt, puis relance des commandes payées
    /// qui attendaient ce produit. Utilisé par la réception fournisseur et par
    /// l'intégration d'un surplus validé (§30).
    /// </summary>
    internal static class EntreeStockHelper
    {
        /// <summary>Physique + q et mouvement Entree. N'enregistre rien.</summary>
        public static async Task<StockProduit> EntrerAsync(
            IApplicationDbContext context, IAuditService audit,
            Guid produitId, Guid entrepotId, int quantite, string reference, string motif,
            CancellationToken cancellationToken)
        {
            var stock = await context.StocksProduit
                .FirstOrDefaultAsync(s => s.ProduitId == produitId && s.EntrepotId == entrepotId, cancellationToken);
            if (stock is null)
            {
                stock = new StockProduit { Id = Guid.NewGuid(), ProduitId = produitId, EntrepotId = entrepotId };
                context.StocksProduit.Add(stock);
            }

            var avant = new { stock.QuantitePhysique, stock.QuantiteReservee };
            stock.QuantitePhysique += quantite;

            context.MouvementsStock.Add(new MouvementStock
            {
                Id = Guid.NewGuid(),
                Type = TypeMouvementStock.Entree,
                Quantite = quantite,
                DateMouvement = DateTime.UtcNow,
                Motif = motif,
                Reference = reference,
                StockProduitId = stock.Id
            });
            audit.Enregistrer("EntreeStock", "StockProduit", stock.Id, avant,
                new { stock.QuantitePhysique, stock.QuantiteReservee, Quantite = quantite, Reference = reference, Motif = motif });

            return stock;
        }

        /// <summary>
        /// Les commandes payées EnAttenteDisponibilite qui contiennent le produit sont relancées
        /// dans l'ordre de paiement : chacune est servie en entier ou pas du tout, et une commande
        /// trop grosse pour le stock reçu ne bloque pas les suivantes. Enregistre si besoin.
        /// </summary>
        public static async Task<List<string>> RelancerCommandesEnAttenteAsync(
            IApplicationDbContext context, IAuditService audit, Guid produitId, CancellationToken cancellationToken)
        {
            var enAttente = await context.Commandes
                .Where(c => c.Statut == StatutCommande.EnAttenteDisponibilite
                            && c.Versions.Any(v => v.NumeroVersion == c.VersionActive && v.Lignes.Any(l => l.ProduitId == produitId)))
                // Premier payé, premier servi
                .OrderBy(c => c.Paiements.Where(p => p.Statut == StatutPaiement.Confirme).Max(p => p.DateConfirmation))
                .ToListAsync(cancellationToken);

            var reservees = new List<string>();
            foreach (var commande in enAttente)
            {
                if (await ReservationStock.TenterAsync(context, audit, commande, cancellationToken))
                    reservees.Add(commande.Reference);
            }

            if (reservees.Count > 0)
                await context.SaveChangesAsync(cancellationToken);

            return reservees;
        }
    }
}
