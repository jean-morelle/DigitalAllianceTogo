using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Models;
using DigitalAllianceTogo.Application.Sav.Common;
using DigitalAllianceTogo.Domain.Enum;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Sav.Queries
{
    public class TicketSavDto
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }
        public string Motif { get; set; } = string.Empty;
        public int Quantite { get; set; }
        public string? Decision { get; set; }
        public string? Resolution { get; set; }
        public DateTime? DateCloture { get; set; }
        public bool AncienProduitReceptionne { get; set; }
        public Guid ClientId { get; set; }
        public string CodeClient { get; set; } = string.Empty;
        public Guid CommandeId { get; set; }
        public string CommandeReference { get; set; } = string.Empty;
        public Guid LigneCommandeId { get; set; }
        public Guid ProduitId { get; set; }
        public string ProduitNom { get; set; } = string.Empty;
        public Guid? TechnicienId { get; set; }
    }

    public class TicketSavDetailDto : TicketSavDto
    {
        public List<DiagnosticDto> Diagnostics { get; set; } = new();
        public List<InterventionDto> Interventions { get; set; } = new();
        public List<SuiviDto> Livraisons { get; set; } = new();
        public List<SuiviDto> Regularisations { get; set; } = new();
    }

    public record DiagnosticDto(DateTime Date, string Conclusion, bool Reparable, string? Recommandation, Guid TechnicienId);
    public record InterventionDto(DateTime DateDebut, DateTime? DateFin, string Description, string? Resultat, Guid TechnicienId);
    public record SuiviDto(Guid Id, string Type, string Reference, string Statut, decimal? Montant);

    /// <summary>Tickets SAV (un client ne voit que les siens), les plus anciens d'abord.</summary>
    public record GetTicketsSavQuery : IRequest<PaginatedList<TicketSavDto>>
    {
        public StatutSav? Statut { get; init; }
        public Guid? CommandeId { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }

    public record GetTicketSavByIdQuery(Guid Id) : IRequest<TicketSavDetailDto>;

    public class GetTicketsSavQueryValidator : AbstractValidator<GetTicketsSavQuery>
    {
        public GetTicketsSavQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    public class SavQueriesHandler :
        IRequestHandler<GetTicketsSavQuery, PaginatedList<TicketSavDto>>,
        IRequestHandler<GetTicketSavByIdQuery, TicketSavDetailDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public SavQueriesHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<PaginatedList<TicketSavDto>> Handle(GetTicketsSavQuery request, CancellationToken cancellationToken)
        {
            var query = _context.TicketsSAV.AsNoTracking().AsQueryable();

            if (!SavHelper.VoitTousLesTickets(_currentUser))
                query = query.Where(t => t.Client.UtilisateurId == _currentUser.UtilisateurId);
            if (request.Statut.HasValue)
                query = query.Where(t => t.Statut == request.Statut.Value);
            if (request.CommandeId.HasValue)
                query = query.Where(t => t.LigneCommande.VersionCommande.CommandeId == request.CommandeId.Value);

            var dtoQuery = query.OrderBy(t => t.DateCreation).Select(t => new TicketSavDto
            {
                Id = t.Id,
                Reference = t.Reference,
                Statut = t.Statut.ToString(),
                DateCreation = t.DateCreation,
                Motif = t.Motif,
                Quantite = t.Quantite,
                Decision = t.Decision == null ? null : t.Decision.ToString(),
                Resolution = t.Resolution,
                DateCloture = t.DateCloture,
                AncienProduitReceptionne = t.AncienProduitReceptionne,
                ClientId = t.ClientId,
                CodeClient = t.Client.CodeClient,
                CommandeId = t.LigneCommande.VersionCommande.CommandeId,
                CommandeReference = t.LigneCommande.VersionCommande.Commande.Reference,
                LigneCommandeId = t.LigneCommandeId,
                ProduitId = t.LigneCommande.ProduitId,
                ProduitNom = t.LigneCommande.Produit.Nom,
                TechnicienId = t.TechnicienId
            });

            return await PaginatedList<TicketSavDto>.CreateAsync(dtoQuery, request.PageNumber, request.PageSize);
        }

        public async Task<TicketSavDetailDto> Handle(GetTicketSavByIdQuery request, CancellationToken cancellationToken)
        {
            // Contrôle d'accès
            await SavHelper.ChargerAvecControleAccesAsync(_context, _currentUser, request.Id, cancellationToken);

            var t = await _context.TicketsSAV.AsNoTracking()
                .Include(x => x.Client)
                .Include(x => x.LigneCommande).ThenInclude(l => l.Produit)
                .Include(x => x.LigneCommande).ThenInclude(l => l.VersionCommande).ThenInclude(v => v.Commande)
                .Include(x => x.Diagnostics)
                .Include(x => x.Interventions)
                .FirstAsync(x => x.Id == request.Id, cancellationToken);

            var livraisons = await _context.Livraisons.AsNoTracking()
                .Where(l => l.TicketSAVId == t.Id)
                .OrderBy(l => l.DatePlanifiee)
                .Select(l => new SuiviDto(l.Id, l.Type.ToString(), l.Reference, l.Statut.ToString(), null))
                .ToListAsync(cancellationToken);
            var remboursements = await _context.Remboursements.AsNoTracking()
                .Where(r => r.TicketSAVId == t.Id)
                .Select(r => new SuiviDto(r.Id, "Remboursement", r.Reference, r.Statut.ToString(), r.Montant))
                .ToListAsync(cancellationToken);
            var avoirs = await _context.Avoirs.AsNoTracking()
                .Where(a => a.TicketSAVId == t.Id)
                .Select(a => new SuiviDto(a.Id, "Avoir", a.Reference, a.Statut.ToString(), a.Montant))
                .ToListAsync(cancellationToken);

            return new TicketSavDetailDto
            {
                Id = t.Id,
                Reference = t.Reference,
                Statut = t.Statut.ToString(),
                DateCreation = t.DateCreation,
                Motif = t.Motif,
                Quantite = t.Quantite,
                Decision = t.Decision?.ToString(),
                Resolution = t.Resolution,
                DateCloture = t.DateCloture,
                AncienProduitReceptionne = t.AncienProduitReceptionne,
                ClientId = t.ClientId,
                CodeClient = t.Client.CodeClient,
                CommandeId = t.LigneCommande.VersionCommande.CommandeId,
                CommandeReference = t.LigneCommande.VersionCommande.Commande.Reference,
                LigneCommandeId = t.LigneCommandeId,
                ProduitId = t.LigneCommande.ProduitId,
                ProduitNom = t.LigneCommande.Produit.Nom,
                TechnicienId = t.TechnicienId,
                Diagnostics = t.Diagnostics.OrderBy(d => d.DateDiagnostic)
                    .Select(d => new DiagnosticDto(d.DateDiagnostic, d.Conclusion, d.Reparable, d.Recommandation, d.TechnicienId)).ToList(),
                Interventions = t.Interventions.OrderBy(i => i.DateDebut)
                    .Select(i => new InterventionDto(i.DateDebut, i.DateFin, i.Description, i.Resultat, i.TechnicienId)).ToList(),
                Livraisons = livraisons,
                Regularisations = remboursements.Concat(avoirs).ToList()
            };
        }
    }
}
