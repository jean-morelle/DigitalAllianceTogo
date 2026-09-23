using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Models;
using DigitalAllianceTogo.Application.Livraisons.Common;
using DigitalAllianceTogo.Domain.Enum;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Livraisons.Queries
{
    public class LivraisonDto
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
        public DateTime DatePlanifiee { get; set; }
        public DateTime? DatePriseEnCharge { get; set; }
        public DateTime? DateLivraison { get; set; }
        public string? MotifEchec { get; set; }
        public string? Reserve { get; set; }
        public Guid CommandeId { get; set; }
        public string CommandeReference { get; set; } = string.Empty;
        public Guid? TicketSAVId { get; set; }
        public Guid? LivreurId { get; set; }
        public string? LivreurNom { get; set; }

        // Ce dont le livreur a besoin sur le terrain
        public string AdresseLigne1 { get; set; } = string.Empty;
        public string? AdresseLigne2 { get; set; }
        public string Ville { get; set; } = string.Empty;
        public string TelephoneContact { get; set; } = string.Empty;

        public PreuveLivraisonDto? Preuve { get; set; }
    }

    public record PreuveLivraisonDto(DateTime DatePreuve, string? PhotoUrl, string? SignatureUrl, decimal? Latitude, decimal? Longitude, string? Commentaire);

    /// <summary>Livraisons (un livreur ne voit que les siennes), les plus proches d'abord.</summary>
    public record GetLivraisonsQuery : IRequest<PaginatedList<LivraisonDto>>
    {
        public StatutLivraison? Statut { get; init; }
        public Guid? LivreurId { get; init; }
        public Guid? CommandeId { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }

    public record GetLivraisonByIdQuery(Guid Id) : IRequest<LivraisonDto>;

    public class GetLivraisonsQueryValidator : AbstractValidator<GetLivraisonsQuery>
    {
        public GetLivraisonsQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    public class LivraisonQueriesHandler :
        IRequestHandler<GetLivraisonsQuery, PaginatedList<LivraisonDto>>,
        IRequestHandler<GetLivraisonByIdQuery, LivraisonDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public LivraisonQueriesHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<PaginatedList<LivraisonDto>> Handle(GetLivraisonsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Livraisons.AsNoTracking().AsQueryable();

            if (!LivraisonHelper.VoitToutesLesLivraisons(_currentUser))
                query = query.Where(l => l.LivreurId == _currentUser.UtilisateurId);
            else if (request.LivreurId.HasValue)
                query = query.Where(l => l.LivreurId == request.LivreurId.Value);

            if (request.Statut.HasValue)
                query = query.Where(l => l.Statut == request.Statut.Value);
            if (request.CommandeId.HasValue)
                query = query.Where(l => l.CommandeId == request.CommandeId.Value);

            return await PaginatedList<LivraisonDto>.CreateAsync(Projeter(query.OrderBy(l => l.DatePlanifiee)), request.PageNumber, request.PageSize);
        }

        public async Task<LivraisonDto> Handle(GetLivraisonByIdQuery request, CancellationToken cancellationToken)
        {
            // Contrôle d'accès (livreur assigné ou Admin / Gestionnaire de stock)
            await LivraisonHelper.ChargerAvecControleAccesAsync(_context, _currentUser, request.Id, cancellationToken);

            return await Projeter(_context.Livraisons.AsNoTracking().Where(l => l.Id == request.Id)).FirstAsync(cancellationToken);
        }

        private static IQueryable<LivraisonDto> Projeter(IQueryable<Domain.Models.Livraison.Livraison> query) =>
            query.Select(l => new LivraisonDto
            {
                Id = l.Id,
                Reference = l.Reference,
                Type = l.Type.ToString(),
                Statut = l.Statut.ToString(),
                DatePlanifiee = l.DatePlanifiee,
                DatePriseEnCharge = l.DatePriseEnCharge,
                DateLivraison = l.DateLivraison,
                MotifEchec = l.MotifEchec,
                Reserve = l.Reserve,
                CommandeId = l.CommandeId,
                CommandeReference = l.Commande.Reference,
                TicketSAVId = l.TicketSAVId,
                LivreurId = l.LivreurId,
                LivreurNom = l.Livreur == null ? null : l.Livreur.Prenom + " " + l.Livreur.Nom,
                AdresseLigne1 = l.Commande.AdresseLivraison.Ligne1,
                AdresseLigne2 = l.Commande.AdresseLivraison.Ligne2,
                Ville = l.Commande.AdresseLivraison.Ville,
                TelephoneContact = l.Commande.AdresseLivraison.TelephoneContact,
                Preuve = l.Preuve == null ? null : new PreuveLivraisonDto(
                    l.Preuve.DatePreuve, l.Preuve.PhotoUrl, l.Preuve.SignatureUrl, l.Preuve.Latitude, l.Preuve.Longitude, l.Preuve.Commentaire)
            });
    }
}
