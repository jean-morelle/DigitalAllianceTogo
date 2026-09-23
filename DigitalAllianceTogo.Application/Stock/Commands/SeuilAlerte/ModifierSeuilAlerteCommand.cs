using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Stock.Commands.SeuilAlerte
{
    /// <summary>
    /// Seuil d'alerte d'un produit dans un entrepôt : quand le disponible (physique − réservé)
    /// passe à ce niveau ou en dessous, il apparaît dans « produits sous le seuil ».
    /// 0 = pas d'alerte.
    /// </summary>
    public record ModifierSeuilAlerteCommand : IRequest
    {
        public Guid StockProduitId { get; init; }
        public int SeuilAlerte { get; init; }
    }

    public class ModifierSeuilAlerteCommandValidator : AbstractValidator<ModifierSeuilAlerteCommand>
    {
        public ModifierSeuilAlerteCommandValidator()
        {
            RuleFor(x => x.StockProduitId).NotEmpty();
            RuleFor(x => x.SeuilAlerte).InclusiveBetween(0, 100_000);
        }
    }

    public class ModifierSeuilAlerteCommandHandler : IRequestHandler<ModifierSeuilAlerteCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAuditService _audit;

        public ModifierSeuilAlerteCommandHandler(IApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task Handle(ModifierSeuilAlerteCommand request, CancellationToken cancellationToken)
        {
            var stock = await _context.StocksProduit.FirstOrDefaultAsync(s => s.Id == request.StockProduitId, cancellationToken)
                ?? throw new NotFoundException("StockProduit", request.StockProduitId);

            var avant = stock.SeuilAlerte;
            stock.SeuilAlerte = request.SeuilAlerte;
            _audit.Enregistrer("ModificationSeuilAlerte", "StockProduit", stock.Id,
                new { SeuilAlerte = avant }, new { stock.SeuilAlerte });

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
