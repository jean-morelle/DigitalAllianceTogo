using DigitalAllianceTogo.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Categories.Queries.GetCategories
{
    public record CategorieDto(Guid Id, string Nom, string Description, bool Actif);

    /// <summary>Liste complète, non paginée : sert à peupler des listes déroulantes
    /// (création/édition de produit), pas à afficher un catalogue de catégories.</summary>
    public record GetCategoriesQuery : IRequest<List<CategorieDto>>;
    public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, List<CategorieDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetCategoriesQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CategorieDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
        {
            return await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Nom)
                .Select(c => new CategorieDto(c.Id, c.Nom, c.Description, c.Actif))
                .ToListAsync(cancellationToken);
        }
    }
}
