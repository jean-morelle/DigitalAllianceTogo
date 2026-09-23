using DigitalAllianceTogo.Application.Commandes.Common;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Models;
using DigitalAllianceTogo.Domain.Enum;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Finance.Queries
{
    public class RemboursementDto
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public decimal Montant { get; set; }
        public string Statut { get; set; } = string.Empty;
        public string Motif { get; set; } = string.Empty;
        public DateTime DateDemande { get; set; }
        public DateTime? DateExecution { get; set; }
        public string? ReferenceTransaction { get; set; }
        public string? MotifEchec { get; set; }
        public Guid CommandeId { get; set; }
        public string CommandeReference { get; set; } = string.Empty;
        public int NumeroVersion { get; set; }
    }

    public class AvoirDto
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public decimal Montant { get; set; }
        public string Statut { get; set; } = string.Empty;
        public string Motif { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }
        public DateTime? DateUtilisation { get; set; }
        public Guid CommandeId { get; set; }
        public string CommandeReference { get; set; } = string.Empty;
        public int NumeroVersion { get; set; }
    }

    /// <summary>Remboursements (le personnel voit tout, un client les siens), les plus anciens d'abord.</summary>
    public record GetRemboursementsQuery : IRequest<PaginatedList<RemboursementDto>>
    {
        public StatutRemboursement? Statut { get; init; }
        public Guid? CommandeId { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }

    /// <summary>Avoirs (le personnel voit tout, un client les siens), les plus anciens d'abord.</summary>
    public record GetAvoirsQuery : IRequest<PaginatedList<AvoirDto>>
    {
        public StatutAvoir? Statut { get; init; }
        public Guid? CommandeId { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }

    public class GetRemboursementsQueryValidator : AbstractValidator<GetRemboursementsQuery>
    {
        public GetRemboursementsQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    public class GetAvoirsQueryValidator : AbstractValidator<GetAvoirsQuery>
    {
        public GetAvoirsQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    public class RegularisationQueriesHandler :
        IRequestHandler<GetRemboursementsQuery, PaginatedList<RemboursementDto>>,
        IRequestHandler<GetAvoirsQuery, PaginatedList<AvoirDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public RegularisationQueriesHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<PaginatedList<RemboursementDto>> Handle(GetRemboursementsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Remboursements.AsNoTracking().AsQueryable();

            if (!CommandeHelper.VoitToutesLesCommandes(_currentUser))
                query = query.Where(r => r.Commande.Client.UtilisateurId == _currentUser.UtilisateurId);
            if (request.Statut.HasValue)
                query = query.Where(r => r.Statut == request.Statut.Value);
            if (request.CommandeId.HasValue)
                query = query.Where(r => r.CommandeId == request.CommandeId.Value);

            var dtoQuery = query.OrderBy(r => r.DateDemande).Select(r => new RemboursementDto
            {
                Id = r.Id,
                Reference = r.Reference,
                Montant = r.Montant,
                Statut = r.Statut.ToString(),
                Motif = r.Motif,
                DateDemande = r.DateDemande,
                DateExecution = r.DateExecution,
                ReferenceTransaction = r.ReferenceTransaction,
                MotifEchec = r.MotifEchec,
                CommandeId = r.CommandeId,
                CommandeReference = r.Commande.Reference,
                NumeroVersion = r.VersionCommande.NumeroVersion
            });

            return await PaginatedList<RemboursementDto>.CreateAsync(dtoQuery, request.PageNumber, request.PageSize);
        }

        public async Task<PaginatedList<AvoirDto>> Handle(GetAvoirsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Avoirs.AsNoTracking().AsQueryable();

            if (!CommandeHelper.VoitToutesLesCommandes(_currentUser))
                query = query.Where(a => a.Commande.Client.UtilisateurId == _currentUser.UtilisateurId);
            if (request.Statut.HasValue)
                query = query.Where(a => a.Statut == request.Statut.Value);
            if (request.CommandeId.HasValue)
                query = query.Where(a => a.CommandeId == request.CommandeId.Value);

            var dtoQuery = query.OrderBy(a => a.DateCreation).Select(a => new AvoirDto
            {
                Id = a.Id,
                Reference = a.Reference,
                Montant = a.Montant,
                Statut = a.Statut.ToString(),
                Motif = a.Motif,
                DateCreation = a.DateCreation,
                DateUtilisation = a.DateUtilisation,
                CommandeId = a.CommandeId,
                CommandeReference = a.Commande.Reference,
                NumeroVersion = a.VersionCommande.NumeroVersion
            });

            return await PaginatedList<AvoirDto>.CreateAsync(dtoQuery, request.PageNumber, request.PageSize);
        }
    }
}
