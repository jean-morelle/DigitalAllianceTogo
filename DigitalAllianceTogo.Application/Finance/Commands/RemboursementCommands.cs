using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Finance.Common;
using DigitalAllianceTogo.Application.Sav.Common;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Finance;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Finance.Commands
{
    /// <summary>L'Administrateur valide la demande de remboursement (§22) : En attente → Validé.</summary>
    public record ValiderRemboursementCommand(Guid Id) : IRequest;

    /// <summary>
    /// Résultat de l'envoi de l'argent : Exécuté (référence du transfert obligatoire)
    /// ou Échoué (raison obligatoire). Un remboursement échoué peut être réexécuté ;
    /// tant qu'il n'est pas exécuté, la commande ne peut pas être clôturée.
    /// </summary>
    public record ExecuterRemboursementCommand : IRequest
    {
        public Guid Id { get; init; }
        public bool Reussi { get; init; }
        public string? ReferenceTransaction { get; init; }
        public string? MotifEchec { get; init; }
    }

    public class ExecuterRemboursementCommandValidator : AbstractValidator<ExecuterRemboursementCommand>
    {
        public ExecuterRemboursementCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.ReferenceTransaction)
                .NotEmpty().When(x => x.Reussi).WithMessage("La référence du transfert est obligatoire.")
                .MaximumLength(100);
            RuleFor(x => x.MotifEchec)
                .NotEmpty().When(x => !x.Reussi).WithMessage("Indiquez pourquoi le remboursement a échoué.")
                .MaximumLength(500);
        }
    }

    public class RemboursementCommandsHandler : IRequestHandler<ValiderRemboursementCommand>, IRequestHandler<ExecuterRemboursementCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAuditService _audit;

        public RemboursementCommandsHandler(IApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task Handle(ValiderRemboursementCommand request, CancellationToken cancellationToken)
        {
            var remboursement = await ChargerAsync(request.Id, cancellationToken);
            if (remboursement.Statut != StatutRemboursement.EnAttente)
                throw new ConflictException($"Ce remboursement est au statut {remboursement.Statut} : il ne peut pas être validé.");

            remboursement.Statut = StatutRemboursement.Valide;
            _audit.Enregistrer("ValidationRemboursement", "Remboursement", remboursement.Id,
                new { Statut = StatutRemboursement.EnAttente.ToString() }, new { Statut = remboursement.Statut.ToString(), remboursement.Montant });

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task Handle(ExecuterRemboursementCommand request, CancellationToken cancellationToken)
        {
            var remboursement = await ChargerAsync(request.Id, cancellationToken);
            if (remboursement.Statut is not (StatutRemboursement.Valide or StatutRemboursement.Echoue))
                throw new ConflictException(remboursement.Statut == StatutRemboursement.EnAttente
                    ? "Ce remboursement doit d'abord être validé par l'Administrateur."
                    : $"Ce remboursement est au statut {remboursement.Statut}.");

            var avant = new { Statut = remboursement.Statut.ToString(), remboursement.MotifEchec };
            if (request.Reussi)
            {
                remboursement.Statut = StatutRemboursement.Execute;
                remboursement.ReferenceTransaction = request.ReferenceTransaction!.Trim();
                remboursement.DateExecution = DateTime.UtcNow;
                remboursement.MotifEchec = null;
            }
            else
            {
                remboursement.Statut = StatutRemboursement.Echoue;
                remboursement.MotifEchec = request.MotifEchec!.Trim();
            }

            _audit.Enregistrer(request.Reussi ? "ExecutionRemboursement" : "EchecRemboursement", "Remboursement", remboursement.Id, avant, new
            {
                Statut = remboursement.Statut.ToString(),
                remboursement.Montant,
                remboursement.ReferenceTransaction,
                remboursement.MotifEchec
            });

            await RegularisationFinanciere.CloturerSiRegulariseeAsync(_context, _audit, remboursement.Commande, cancellationToken);
            if (remboursement.TicketSAVId is Guid ticketId)
                await SavHelper.CloturerSiRegulariseAsync(_context, _audit, ticketId, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task<Remboursement> ChargerAsync(Guid id, CancellationToken cancellationToken) =>
            await _context.Remboursements.Include(r => r.Commande).FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("Remboursement", id);
    }
}
