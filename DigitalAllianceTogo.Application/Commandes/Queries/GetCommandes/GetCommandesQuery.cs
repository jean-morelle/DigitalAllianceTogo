using DigitalAllianceTogo.Application.Commandes.Dtos;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Models;
using DigitalAllianceTogo.Application.Commandes.Common;
using DigitalAllianceTogo.Domain.Enum;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Commandes.Queries.GetCommandes
{
    /// <summary>
    /// Liste paginée des commandes. Le personnel voit tout ;
    /// un client ne voit QUE les siennes, quels que soient les filtres envoyés.
    /// </summary>
    public record GetCommandesQuery : IRequest<PaginatedList<CommandeDto>>
    {
        public string? Recherche { get; init; }
        public StatutCommande? Statut { get; init; }
        public Guid? ClientId { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }

    public class GetCommandesQueryValidator : AbstractValidator<GetCommandesQuery>
    {
        public GetCommandesQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    public class GetCommandesQueryHandler : IRequestHandler<GetCommandesQuery, PaginatedList<CommandeDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetCommandesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<PaginatedList<CommandeDto>> Handle(GetCommandesQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Commandes.AsNoTracking().AsQueryable();

            if (!CommandeHelper.VoitToutesLesCommandes(_currentUser))
                query = query.Where(c => c.Client.UtilisateurId == _currentUser.UtilisateurId);
            else if (request.ClientId.HasValue)
                query = query.Where(c => c.ClientId == request.ClientId.Value);

            if (!string.IsNullOrWhiteSpace(request.Recherche))
            {
                var terme = request.Recherche.Trim();
                query = query.Where(c => c.Reference.Contains(terme) || c.Client.CodeClient.Contains(terme));
            }

            if (request.Statut.HasValue)
                query = query.Where(c => c.Statut == request.Statut.Value);

            var dtoQuery = query
                .OrderByDescending(c => c.DateCreation)
                .Select(c => new CommandeDto
                {
                    Id = c.Id,
                    Reference = c.Reference,
                    Statut = c.Statut.ToString(),
                    DateCreation = c.DateCreation,
                    VersionActive = c.VersionActive,
                    Total = c.Versions.Where(v => v.NumeroVersion == c.VersionActive).Select(v => v.Total).FirstOrDefault(),
                    ClientId = c.ClientId,
                    CodeClient = c.Client.CodeClient
                });

            return await PaginatedList<CommandeDto>.CreateAsync(dtoQuery, request.PageNumber, request.PageSize);
        }
    }
}
