using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Models;
using DigitalAllianceTogo.Application.Utilisateurs.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Utilisateurs.Queries.GetUtilisateurs
{
    public record GetUtilisateursQuery : IRequest<PaginatedList<UtilisateurDto>>
    {
        public string? Recherche { get; init; } // filtre libre sur Nom/Prenom/Email
        public bool? Actif { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }
    public class GetUtilisateursQueryHandler : IRequestHandler<GetUtilisateursQuery, PaginatedList<UtilisateurDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetUtilisateursQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<UtilisateurDto>> Handle(GetUtilisateursQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Utilisateurs
                .Include(u => u.UtilisateurRoles)
                    .ThenInclude(ur => ur.Role)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Recherche))
            {
                var terme = request.Recherche.Trim();
                query = query.Where(u =>
                    u.Nom.Contains(terme) ||
                    u.Prenom.Contains(terme) ||
                    u.Email.Contains(terme));
            }

            if (request.Actif.HasValue)
                query = query.Where(u => u.Actif == request.Actif.Value);

            query = query.OrderBy(u => u.Nom).ThenBy(u => u.Prenom);

            var dtoQuery = query.Select(u => new UtilisateurDto
            {
                Id = u.Id,
                Nom = u.Nom,
                Prenom = u.Prenom,
                Email = u.Email,
                Telephone = u.Telephone,
                Actif = u.Actif,
                DateCreation = u.DateCreation,
                Roles = u.UtilisateurRoles.Select(ur => ur.Role.Nom).ToList()
            });

            return await PaginatedList<UtilisateurDto>.CreateAsync(dtoQuery, request.PageNumber, request.PageSize);
        }
    }
}
