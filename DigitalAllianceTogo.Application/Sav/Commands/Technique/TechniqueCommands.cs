using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Sav.Common;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.SAV;
using FluentValidation;
using MediatR;

namespace DigitalAllianceTogo.Application.Sav.Commands.Technique
{
    /// <summary>
    /// Le Technicien diagnostique le produit (§26) :
    /// réparable → En réparation ; irréparable → Décision commerciale (recommandation obligatoire).
    /// Le Technicien ne prend aucune décision financière.
    /// </summary>
    public record DiagnostiquerTicketCommand : IRequest
    {
        public Guid Id { get; init; }
        public string Conclusion { get; init; } = string.Empty;
        public bool Reparable { get; init; }
        public string? Recommandation { get; init; }
    }

    /// <summary>
    /// Fin de réparation avec test : réussi → ticket clôturé techniquement (le Commercial
    /// informe le client, pas de validation Admin) ; raté → Décision commerciale.
    /// </summary>
    public record TerminerReparationCommand : IRequest
    {
        public Guid Id { get; init; }
        public string Description { get; init; } = string.Empty;
        public string? Resultat { get; init; }
        public bool TestReussi { get; init; }
    }

    public class DiagnostiquerTicketCommandValidator : AbstractValidator<DiagnostiquerTicketCommand>
    {
        public DiagnostiquerTicketCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Conclusion).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.Recommandation).MaximumLength(2000);
            RuleFor(x => x.Recommandation)
                .NotEmpty().When(x => !x.Reparable)
                .WithMessage("Un produit irréparable exige une recommandation (ex : remplacement).");
        }
    }

    public class TerminerReparationCommandValidator : AbstractValidator<TerminerReparationCommand>
    {
        public TerminerReparationCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Description).NotEmpty().WithMessage("Décrivez l'intervention réalisée.").MaximumLength(2000);
            RuleFor(x => x.Resultat).MaximumLength(2000);
        }
    }

    public class TechniqueCommandsHandler : IRequestHandler<DiagnostiquerTicketCommand>, IRequestHandler<TerminerReparationCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditService _audit;

        public TechniqueCommandsHandler(IApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
        {
            _context = context;
            _currentUser = currentUser;
            _audit = audit;
        }

        public async Task Handle(DiagnostiquerTicketCommand request, CancellationToken cancellationToken)
        {
            var ticket = await SavHelper.ChargerAvecControleAccesAsync(_context, _currentUser, request.Id, cancellationToken);
            SavHelper.ExigerStatut(ticket, StatutSav.Ouvert);

            var technicienId = _currentUser.UtilisateurId!.Value;
            _context.Diagnostics.Add(new Diagnostic
            {
                Id = Guid.NewGuid(),
                DateDiagnostic = DateTime.UtcNow,
                Conclusion = request.Conclusion.Trim(),
                Reparable = request.Reparable,
                Recommandation = request.Recommandation?.Trim(),
                TicketSAVId = ticket.Id,
                TechnicienId = technicienId
            });
            ticket.TechnicienId = technicienId;

            SavHelper.ChangerStatut(_audit, ticket,
                request.Reparable ? StatutSav.EnReparation : StatutSav.DecisionCommerciale,
                "DiagnosticSav", new { request.Conclusion, request.Reparable, request.Recommandation });

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task Handle(TerminerReparationCommand request, CancellationToken cancellationToken)
        {
            var ticket = await SavHelper.ChargerAvecControleAccesAsync(_context, _currentUser, request.Id, cancellationToken);
            SavHelper.ExigerStatut(ticket, StatutSav.EnReparation);

            var technicienId = _currentUser.UtilisateurId!.Value;
            _context.Interventions.Add(new Intervention
            {
                Id = Guid.NewGuid(),
                DateDebut = DateTime.UtcNow,
                DateFin = DateTime.UtcNow,
                Description = request.Description.Trim(),
                Resultat = request.Resultat?.Trim(),
                TicketSAVId = ticket.Id,
                TechnicienId = technicienId
            });
            ticket.TechnicienId = technicienId;

            if (request.TestReussi)
                SavHelper.Cloturer(_audit, ticket, "Réparé et testé");
            else
                SavHelper.ChangerStatut(_audit, ticket, StatutSav.DecisionCommerciale, "EchecReparationSav",
                    new { request.Description, request.Resultat });

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
