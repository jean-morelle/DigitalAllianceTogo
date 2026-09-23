using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Models;
using DigitalAllianceTogo.Application.Devis.Common;
using DigitalAllianceTogo.Application.Devis.Dtos;
using DigitalAllianceTogo.Domain.Enum;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Devis.Queries.GetDevis
{
    /// <summary>
    /// Liste paginée des devis. Le personnel voit tous les devis ;
    /// un client ne voit QUE les siens, quels que soient les filtres envoyés.
    /// </summary>
    public record GetDevisQuery : IRequest<PaginatedList<DevisDto>>
    {
        public string? Recherche { get; init; }
        public StatutDevis? Statut { get; init; }
        public Guid? ClientId { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }

    public class GetDevisQueryHandler : IRequestHandler<GetDevisQuery, PaginatedList<DevisDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetDevisQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<PaginatedList<DevisDto>> Handle(GetDevisQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Devis.AsNoTracking().AsQueryable();

            if (!DevisHelper.EstPersonnel(_currentUser))
                query = query.Where(d => d.Client.UtilisateurId == _currentUser.UtilisateurId);
            else if (request.ClientId.HasValue)
                query = query.Where(d => d.ClientId == request.ClientId.Value);

            if (!string.IsNullOrWhiteSpace(request.Recherche))
            {
                var terme = request.Recherche.Trim();
                query = query.Where(d => d.Reference.Contains(terme) || d.Client.CodeClient.Contains(terme));
            }

            if (request.Statut.HasValue)
                query = query.Where(d => d.Statut == request.Statut.Value);

            var dtoQuery = query
                .OrderByDescending(d => d.DateCreation)
                .Select(d => new DevisDto
                {
                    Id = d.Id,
                    Reference = d.Reference,
                    Statut = d.Statut.ToString(),
                    DateCreation = d.DateCreation,
                    DateValidite = d.DateValidite,
                    SousTotal = d.SousTotal,
                    Remise = d.Remise,
                    Total = d.Total,
                    ValideParEntreprise = d.ValideParId != null,
                    ClientId = d.ClientId,
                    CodeClient = d.Client.CodeClient
                });

            return await PaginatedList<DevisDto>.CreateAsync(dtoQuery, request.PageNumber, request.PageSize);
        }
    }
}
