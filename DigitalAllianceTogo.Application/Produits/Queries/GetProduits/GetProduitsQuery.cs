using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Models;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Produits.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Produits.Queries.GetProduits
{
    public record GetProduitsQuery : IRequest<PaginatedList<ProduitDto>>
    {
        public string? Recherche { get; init; }
        public Guid? CategorieId { get; init; }
        public Guid? MarqueId { get; init; }
        public bool? Actif { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }

    public class GetProduitsQueryHandler : IRequestHandler<GetProduitsQuery, PaginatedList<ProduitDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _user;

        public GetProduitsQueryHandler(IApplicationDbContext context, ICurrentUserService user)
        {
            _context = context;
            _user = user;
        }

        public async Task<PaginatedList<ProduitDto>> Handle(GetProduitsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Produits
                .Include(p => p.Categorie)
                .Include(p => p.Marque)
                .Include(p => p.Images)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Recherche))
            {
                // Insensible à la casse : « hp » trouve « HP 250 G9 »
                var terme = request.Recherche.Trim().ToLower();
                query = query.Where(p => p.Nom.ToLower().Contains(terme) || p.Reference.ToLower().Contains(terme));
            }

            if (request.CategorieId.HasValue)
                query = query.Where(p => p.CategorieId == request.CategorieId.Value);

            if (request.MarqueId.HasValue)
                query = query.Where(p => p.MarqueId == request.MarqueId.Value);

            // Produits retirés de la vente, ou dont la catégorie ou la marque l'est :
            // visibles seulement de l'Administrateur et du Catalogue
            if (!_user.EstDansRole(Roles.Admin) && !_user.EstDansRole(Roles.Catalogue))
                query = query.Where(p => p.Actif && p.Categorie.Actif && p.Marque.Actif);
            else if (request.Actif.HasValue)
                query = query.Where(p => p.Actif == request.Actif.Value);

            query = query.OrderBy(p => p.Nom);

            var dtoQuery = query.Select(p => new ProduitDto
            {
                Id = p.Id,
                Reference = p.Reference,
                Nom = p.Nom,
                Prix = p.Prix,
                Actif = p.Actif,
                DateCreation = p.DateCreation,
                CategorieNom = p.Categorie.Nom,
                MarqueNom = p.Marque.Nom,
                ImagePrincipaleUrl = p.Images.Where(i => i.EstPrincipale).Select(i => i.Url).FirstOrDefault(),
                EnStock = p.Stocks.Any(s => s.Entrepot.Actif && s.QuantitePhysique - s.QuantiteReservee > 0)
            });

            return await PaginatedList<ProduitDto>.CreateAsync(dtoQuery, request.PageNumber, request.PageSize);
        }
    }
}
