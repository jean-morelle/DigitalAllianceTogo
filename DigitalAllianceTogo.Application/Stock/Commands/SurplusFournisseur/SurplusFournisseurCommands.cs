using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Stock.Common;
using DigitalAllianceTogo.Domain.Enum;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Stock.Commands.SurplusFournisseur
{
    public enum DecisionEcartReception
    {
        IntegrerAuStock = 1,
        RetournerAuFournisseur = 2
    }

    /// <summary>
    /// L'Administrateur tranche un surplus fournisseur (§30) : intégration au stock
    /// (entrée + relance des commandes en attente) ou retour au fournisseur (rien n'entre).
    /// </summary>
    public record DeciderEcartReceptionCommand : IRequest<List<string>>
    {
        public Guid Id { get; init; }
        public DecisionEcartReception Decision { get; init; }
        public string Motif { get; init; } = string.Empty;
    }

    public class DeciderEcartReceptionCommandValidator : AbstractValidator<DeciderEcartReceptionCommand>
    {
        public DeciderEcartReceptionCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Decision).IsInEnum();
            RuleFor(x => x.Motif).NotEmpty().WithMessage("Justifiez la décision (accord du fournisseur, facture...).").MaximumLength(500);
        }
    }

    public record EcartReceptionDto(
        Guid Id, string Reference, Guid ProduitId, string ProduitNom, Guid EntrepotId, string EntrepotNom,
        int QuantiteCommandee, int QuantiteRecue, int Surplus, string Statut, DateTime DateConstat,
        DateTime? DateDecision, string? MotifDecision);

    public record GetEcartsReceptionQuery(StatutEcartReception? Statut = StatutEcartReception.EnAttenteDecision) : IRequest<List<EcartReceptionDto>>;

    public class EcartReceptionHandler :
        IRequestHandler<DeciderEcartReceptionCommand, List<string>>,
        IRequestHandler<GetEcartsReceptionQuery, List<EcartReceptionDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditService _audit;

        public EcartReceptionHandler(IApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
        {
            _context = context;
            _currentUser = currentUser;
            _audit = audit;
        }

        public async Task<List<string>> Handle(DeciderEcartReceptionCommand request, CancellationToken cancellationToken)
        {
            var ecart = await _context.EcartsReception.FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException("EcartReception", request.Id);

            if (ecart.Statut != StatutEcartReception.EnAttenteDecision)
                throw new ConflictException($"Cet écart est déjà traité ({ecart.Statut}).");

            var motif = request.Motif.Trim();
            ecart.DecideParId = _currentUser.UtilisateurId;
            ecart.DateDecision = DateTime.UtcNow;
            ecart.MotifDecision = motif;

            if (request.Decision == DecisionEcartReception.IntegrerAuStock)
            {
                ecart.Statut = StatutEcartReception.IntegreAuStock;
                await EntreeStockHelper.EntrerAsync(_context, _audit, ecart.ProduitId, ecart.EntrepotId, ecart.Surplus,
                    ecart.Reference, $"Surplus fournisseur validé : {motif}", cancellationToken);
            }
            else
            {
                ecart.Statut = StatutEcartReception.RetourneFournisseur;
            }

            _audit.Enregistrer("DecisionSurplusFournisseur", "EcartReception", ecart.Id,
                new { Statut = StatutEcartReception.EnAttenteDecision.ToString() },
                new { Statut = ecart.Statut.ToString(), ecart.Surplus, Motif = motif });

            // xmin : deux décisions simultanées ne peuvent pas intégrer le surplus deux fois
            await _context.SaveChangesAsync(cancellationToken);

            return request.Decision == DecisionEcartReception.IntegrerAuStock
                ? await EntreeStockHelper.RelancerCommandesEnAttenteAsync(_context, _audit, ecart.ProduitId, cancellationToken)
                : new List<string>();
        }

        public async Task<List<EcartReceptionDto>> Handle(GetEcartsReceptionQuery request, CancellationToken cancellationToken)
        {
            var query = _context.EcartsReception.AsNoTracking().AsQueryable();
            if (request.Statut.HasValue)
                query = query.Where(e => e.Statut == request.Statut.Value);

            return await query
                .OrderBy(e => e.DateConstat)
                .Select(e => new EcartReceptionDto(
                    e.Id, e.Reference, e.ProduitId, e.Produit.Nom, e.EntrepotId, e.Entrepot.Nom,
                    e.QuantiteCommandee, e.QuantiteRecue, e.QuantiteRecue - e.QuantiteCommandee, e.Statut.ToString(), e.DateConstat,
                    e.DateDecision, e.MotifDecision))
                .ToListAsync(cancellationToken);
        }
    }
}
