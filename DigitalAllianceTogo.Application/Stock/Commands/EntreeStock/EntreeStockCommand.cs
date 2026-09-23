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
    /// Réception de marchandise dans un entrepôt (+ QuantitePhysique, mouvement Entree),
    /// puis relance des commandes payées en attente de ce produit.
    ///
    /// Surplus fournisseur (§30) : si l'on indique la quantité commandée et que la livraison
    /// réelle la dépasse, SEULE la quantité commandée entre en stock. L'écart est enregistré
    /// et attend la décision de l'Administrateur (intégration ou retour fournisseur) :
    /// un surplus n'est jamais intégré automatiquement.
    /// </summary>
    public record EntreeStockCommand : IRequest<EntreeStockResult>
    {
        public Guid ProduitId { get; init; }
        public Guid EntrepotId { get; init; }

        /// <summary>Quantité réellement reçue.</summary>
        public int Quantite { get; init; }

        /// <summary>Quantité commandée au fournisseur (facultative : sans elle, tout entre en stock).</summary>
        public int? QuantiteCommandee { get; init; }

        /// <summary>Bon de livraison fournisseur, facture...</summary>
        public string Reference { get; init; } = string.Empty;
        public string? Motif { get; init; }
    }

    public record EntreeStockResult(int QuantitePhysique, int QuantiteDisponible, List<string> CommandesReservees, int SurplusEnAttente, Guid? EcartId);

    public class EntreeStockCommandValidator : AbstractValidator<EntreeStockCommand>
    {
        public EntreeStockCommandValidator()
        {
            RuleFor(x => x.ProduitId).NotEmpty();
            RuleFor(x => x.EntrepotId).NotEmpty();
            RuleFor(x => x.Quantite).InclusiveBetween(1, 100_000);
            RuleFor(x => x.QuantiteCommandee).InclusiveBetween(1, 100_000);
            RuleFor(x => x.Reference).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Motif).MaximumLength(500);
        }
    }

    public class EntreeStockCommandHandler : IRequestHandler<EntreeStockCommand, EntreeStockResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditService _audit;

        public EntreeStockCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
        {
            _context = context;
            _currentUser = currentUser;
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

            var reference = request.Reference.Trim();
            var surplus = request.QuantiteCommandee is int commandee && request.Quantite > commandee
                ? request.Quantite - commandee
                : 0;
            var aIntegrer = request.Quantite - surplus;

            var stock = await EntreeStockHelper.EntrerAsync(_context, _audit, request.ProduitId, request.EntrepotId, aIntegrer, reference,
                string.IsNullOrWhiteSpace(request.Motif) ? "Réception fournisseur" : request.Motif.Trim(), cancellationToken);

            EcartReception? ecart = null;
            if (surplus > 0)
            {
                ecart = new EcartReception
                {
                    Id = Guid.NewGuid(),
                    Reference = reference,
                    QuantiteCommandee = request.QuantiteCommandee!.Value,
                    QuantiteRecue = request.Quantite,
                    Statut = StatutEcartReception.EnAttenteDecision,
                    DateConstat = DateTime.UtcNow,
                    ConstateParId = _currentUser.UtilisateurId!.Value,
                    ProduitId = request.ProduitId,
                    EntrepotId = request.EntrepotId
                };
                _context.EcartsReception.Add(ecart);
                _audit.Enregistrer("ConstatSurplusFournisseur", "EcartReception", ecart.Id, apres: new
                {
                    ecart.Reference,
                    ecart.QuantiteCommandee,
                    ecart.QuantiteRecue,
                    Surplus = surplus
                });
            }

            // L'entrée est valable seule : on l'enregistre avant de relancer les commandes
            await _context.SaveChangesAsync(cancellationToken);

            var commandesReservees = await EntreeStockHelper.RelancerCommandesEnAttenteAsync(_context, _audit, request.ProduitId, cancellationToken);

            return new EntreeStockResult(stock.QuantitePhysique, stock.QuantiteDisponible, commandesReservees, surplus, ecart?.Id);
        }
    }
}
