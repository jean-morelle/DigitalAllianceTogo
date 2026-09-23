using DigitalAllianceTogo.Application.Commandes.Dtos;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Models;
using DigitalAllianceTogo.Domain.Enum;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Paiements.Queries.GetPaiements
{
    /// <summary>
    /// Paiements pour le personnel. Par défaut : la file des preuves à vérifier
    /// (EnAttente), les plus anciennes d'abord.
    /// </summary>
    public record GetPaiementsQuery : IRequest<PaginatedList<PaiementDto>>
    {
        public StatutPaiement? Statut { get; init; } = StatutPaiement.EnAttente;
        public string? Recherche { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }

    public class GetPaiementsQueryValidator : AbstractValidator<GetPaiementsQuery>
    {
        public GetPaiementsQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    public class GetPaiementsQueryHandler : IRequestHandler<GetPaiementsQuery, PaginatedList<PaiementDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetPaiementsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<PaiementDto>> Handle(GetPaiementsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Paiements.AsNoTracking().AsQueryable();

            if (request.Statut.HasValue)
                query = query.Where(p => p.Statut == request.Statut.Value);

            if (!string.IsNullOrWhiteSpace(request.Recherche))
            {
                var terme = request.Recherche.Trim().ToUpperInvariant();
                query = query.Where(p => p.Reference.Contains(terme)
                                         || (p.ReferenceExterne != null && p.ReferenceExterne.Contains(terme))
                                         || p.Commande.Reference.Contains(terme));
            }

            var dtoQuery = query
                .OrderBy(p => p.DatePaiement)
                .Select(p => new PaiementDto
                {
                    Id = p.Id,
                    Reference = p.Reference,
                    Montant = p.Montant,
                    Statut = p.Statut.ToString(),
                    Mode = p.Mode.ToString(),
                    DatePaiement = p.DatePaiement,
                    ReferenceExterne = p.ReferenceExterne,
                    PreuveUrl = p.PreuveUrl,
                    DateConfirmation = p.DateConfirmation,
                    MotifRejet = p.MotifRejet,
                    NumeroVersion = p.VersionCommande.NumeroVersion,
                    CommandeId = p.CommandeId,
                    CommandeReference = p.Commande.Reference
                });

            return await PaginatedList<PaiementDto>.CreateAsync(dtoQuery, request.PageNumber, request.PageSize);
        }
    }
}
