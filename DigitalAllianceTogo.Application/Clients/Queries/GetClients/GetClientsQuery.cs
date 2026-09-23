using DigitalAllianceTogo.Application.Clients.Dtos;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Models;
using DigitalAllianceTogo.Domain.Enum;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Clients.Queries.GetClients
{
    /// <summary>Liste paginée des clients (personnel uniquement).</summary>
    public record GetClientsQuery : IRequest<PaginatedList<ClientDto>>
    {
        /// <summary>Nom, raison sociale, téléphone, email ou code client.</summary>
        public string? Recherche { get; init; }
        public TypeClient? Type { get; init; }
        public SourceClient? Source { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }

    public class GetClientsQueryHandler : IRequestHandler<GetClientsQuery, PaginatedList<ClientDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetClientsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<ClientDto>> Handle(GetClientsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Clients.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Recherche))
            {
                var terme = request.Recherche.Trim().ToLower();
                query = query.Where(c =>
                    c.Nom.ToLower().Contains(terme) ||
                    (c.Prenom != null && c.Prenom.ToLower().Contains(terme)) ||
                    (c.RaisonSociale != null && c.RaisonSociale.ToLower().Contains(terme)) ||
                    c.Telephone.Contains(terme) ||
                    (c.Email != null && c.Email.Contains(terme)) ||
                    c.CodeClient.ToLower().Contains(terme));
            }

            if (request.Type.HasValue)
                query = query.Where(c => c.Type == request.Type.Value);

            if (request.Source.HasValue)
                query = query.Where(c => c.Source == request.Source.Value);

            var dtoQuery = query
                .OrderByDescending(c => c.DateCreation)
                .Select(c => new ClientDto
                {
                    Id = c.Id,
                    CodeClient = c.CodeClient,
                    Type = c.Type.ToString(),
                    Nom = c.Nom,
                    Prenom = c.Prenom,
                    RaisonSociale = c.RaisonSociale,
                    Telephone = c.Telephone,
                    Email = c.Email,
                    Source = c.Source.ToString(),
                    DateCreation = c.DateCreation,
                    ACompte = c.UtilisateurId != null
                });

            return await PaginatedList<ClientDto>.CreateAsync(dtoQuery, request.PageNumber, request.PageSize);
        }
    }
}
