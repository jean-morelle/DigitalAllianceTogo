using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Produits.Dtos;
using DigitalAllianceTogo.Domain.Models.Catalogue;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Produits.Queries.GetProduitById
{
    public record GetProduitByIdQuery(Guid Id) : IRequest<ProduitDetailDto>;

    public class GetProduitByIdQueryHandler : IRequestHandler<GetProduitByIdQuery, ProduitDetailDto>
    {
        private readonly IApplicationDbContext _context;

        public GetProduitByIdQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ProduitDetailDto> Handle(GetProduitByIdQuery request, CancellationToken cancellationToken)
        {
            var produit = await _context.Produits
                .Include(p => p.Categorie)
                .Include(p => p.Marque)
                .Include(p => p.Images)
                .Include(p => p.Attributs)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException(nameof(Produit), request.Id);

            var detail = ProduitDetailDto.FromEntity(produit);
            detail.EnStock = await _context.StocksProduit.AnyAsync(s => s.ProduitId == produit.Id && s.Entrepot.Actif
                && s.QuantitePhysique - s.QuantiteReservee > 0, cancellationToken);
            return detail;
        }
    }
}
