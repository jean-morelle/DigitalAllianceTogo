using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Stock.Queries.GetMouvements
{
    /// <summary>Historique des mouvements d'une ligne de stock (§28), le plus récent d'abord.</summary>
    public record GetMouvementsStockQuery : IRequest<PaginatedList<MouvementStockDto>>
    {
        public Guid StockProduitId { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 50;
    }

    public record MouvementStockDto(
        Guid Id, string Type, int Quantite, DateTime DateMouvement, string Motif, string Reference,
        Guid? CommandeId, string? CommandeReference, Guid? TicketSAVId);

    public class GetMouvementsStockQueryValidator : AbstractValidator<GetMouvementsStockQuery>
    {
        public GetMouvementsStockQueryValidator()
        {
            RuleFor(x => x.StockProduitId).NotEmpty();
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
        }
    }

    public class GetMouvementsStockQueryHandler : IRequestHandler<GetMouvementsStockQuery, PaginatedList<MouvementStockDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetMouvementsStockQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public Task<PaginatedList<MouvementStockDto>> Handle(GetMouvementsStockQuery request, CancellationToken cancellationToken)
        {
            var query = _context.MouvementsStock.AsNoTracking()
                .Where(m => m.StockProduitId == request.StockProduitId)
                .OrderByDescending(m => m.DateMouvement)
                .Select(m => new MouvementStockDto(
                    m.Id, m.Type.ToString(), m.Quantite, m.DateMouvement, m.Motif, m.Reference,
                    m.CommandeId, m.Commande == null ? null : m.Commande.Reference, m.TicketSAVId));

            return PaginatedList<MouvementStockDto>.CreateAsync(query, request.PageNumber, request.PageSize);
        }
    }
}
