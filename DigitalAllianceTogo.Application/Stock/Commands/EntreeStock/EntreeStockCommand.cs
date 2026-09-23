using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Stock.Common;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Stock;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Stock.Commands.EntreeStock
{
    /// <summary>
    /// Réception de marchandise dans un entrepôt (+ QuantitePhysique, mouvement Entree).
    /// Ensuite, les commandes payées EnAttenteDisponibilite sont relancées dans l'ordre
    /// de paiement : chacune est servie en entier ou pas du tout, et une commande trop
    /// grosse pour le stock reçu ne bloque pas les suivantes.
    /// </summary>
    public record EntreeStockCommand : IRequest<EntreeStockResult>
    {
        public Guid ProduitId { get; init; }
        public Guid EntrepotId { get; init; }
        public int Quantite { get; init; }

        /// <summary>Bon de livraison fournisseur, facture...</summary>
        public string Reference { get; init; } = string.Empty;
        public string? Motif { get; init; }
    }

    public record EntreeStockResult(int QuantitePhysique, int QuantiteDisponible, List<string> CommandesReservees);

    public class EntreeStockCommandValidator : AbstractValidator<EntreeStockCommand>
    {
        public EntreeStockCommandValidator()
        {
            RuleFor(x => x.ProduitId).NotEmpty();
            RuleFor(x => x.EntrepotId).NotEmpty();
            RuleFor(x => x.Quantite).InclusiveBetween(1, 100_000);
            RuleFor(x => x.Reference).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Motif).MaximumLength(500);
        }
    }

    public class EntreeStockCommandHandler : IRequestHandler<EntreeStockCommand, EntreeStockResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAuditService _audit;

        public EntreeStockCommandHandler(IApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<EntreeStockResult> Handle(EntreeStockCommand request, CancellationToken cancellationToken)
        {
            var entrepot = await _context.Entrepots.AsNoTracking().FirstOrDefaultAsync(e => e.Id == request.EntrepotId, cancellationToken)
                ?? throw new NotFoundException("Entrepot", request.EntrepotId);
            if (!entrepot.Actif)
                throw new ConflictException($"L'entrepôt \"{entrepot.Nom}\" est désactivé.");

            if (!await _context.Produits.AnyAsync(p => p.Id == request.ProduitId, cancellationToken))
                throw new NotFoundException("Produit", request.ProduitId);

            var stock = await _context.StocksProduit
                .FirstOrDefaultAsync(s => s.ProduitId == request.ProduitId && s.EntrepotId == request.EntrepotId, cancellationToken);
            if (stock is null)
            {
                stock = new StockProduit { Id = Guid.NewGuid(), ProduitId = request.ProduitId, EntrepotId = request.EntrepotId };
                _context.StocksProduit.Add(stock);
            }

            var avant = new { stock.QuantitePhysique, stock.QuantiteReservee };
            stock.QuantitePhysique += request.Quantite;

            _context.MouvementsStock.Add(new MouvementStock
            {
                Id = Guid.NewGuid(),
                Type = TypeMouvementStock.Entree,
                Quantite = request.Quantite,
                DateMouvement = DateTime.UtcNow,
                Motif = string.IsNullOrWhiteSpace(request.Motif) ? "Réception fournisseur" : request.Motif.Trim(),
                Reference = request.Reference.Trim(),
                StockProduitId = stock.Id
            });
            _audit.Enregistrer("EntreeStock", "StockProduit", stock.Id, avant,
                new { stock.QuantitePhysique, stock.QuantiteReservee, request.Quantite, request.Reference });

            // L'entrée est valable seule : on l'enregistre avant de relancer les commandes
            await _context.SaveChangesAsync(cancellationToken);

            var commandesReservees = await RelancerCommandesEnAttenteAsync(request.ProduitId, cancellationToken);

            return new EntreeStockResult(stock.QuantitePhysique, stock.QuantiteDisponible, commandesReservees);
        }

        private async Task<List<string>> RelancerCommandesEnAttenteAsync(Guid produitId, CancellationToken cancellationToken)
        {
            var enAttente = await _context.Commandes
                .Where(c => c.Statut == StatutCommande.EnAttenteDisponibilite
                            && c.Versions.Any(v => v.NumeroVersion == c.VersionActive && v.Lignes.Any(l => l.ProduitId == produitId)))
                // Premier payé, premier servi
                .OrderBy(c => c.Paiements.Where(p => p.Statut == StatutPaiement.Confirme).Max(p => p.DateConfirmation))
                .ToListAsync(cancellationToken);

            var reservees = new List<string>();
            foreach (var commande in enAttente)
            {
                if (await ReservationStock.TenterAsync(_context, _audit, commande, cancellationToken))
                    reservees.Add(commande.Reference);
            }

            if (reservees.Count > 0)
                await _context.SaveChangesAsync(cancellationToken);

            return reservees;
        }
    }
}
