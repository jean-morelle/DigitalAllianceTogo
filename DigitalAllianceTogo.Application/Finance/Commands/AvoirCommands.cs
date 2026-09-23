using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Finance.Common;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Finance;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Finance.Commands
{
    /// <summary>
    /// L'Administrateur valide l'avoir (§23) : il est aussitôt mis à la disposition du client
    /// (En attente → Validé → Disponible, les deux étapes sont tracées).
    /// </summary>
    public record ValiderAvoirCommand(Guid Id) : IRequest;

    /// <summary>L'Administrateur annule un avoir non utilisé (motif obligatoire).</summary>
    public record AnnulerAvoirCommand : IRequest
    {
        public Guid Id { get; init; }
        public string Motif { get; init; } = string.Empty;
    }

    public class AnnulerAvoirCommandValidator : AbstractValidator<AnnulerAvoirCommand>
    {
        public AnnulerAvoirCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Motif).NotEmpty().WithMessage("Indiquez pourquoi l'avoir est annulé.").MaximumLength(300);
        }
    }

    public class AvoirCommandsHandler : IRequestHandler<ValiderAvoirCommand>, IRequestHandler<AnnulerAvoirCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAuditService _audit;

        public AvoirCommandsHandler(IApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task Handle(ValiderAvoirCommand request, CancellationToken cancellationToken)
        {
            var avoir = await ChargerAsync(request.Id, cancellationToken);
            if (avoir.Statut != StatutAvoir.EnAttente)
                throw new ConflictException($"Cet avoir est au statut {avoir.Statut} : il ne peut pas être validé.");

            _audit.Enregistrer("ValidationAvoir", "Avoir", avoir.Id,
                new { Statut = StatutAvoir.EnAttente.ToString() }, new { Statut = StatutAvoir.Valide.ToString(), avoir.Montant });
            avoir.Statut = StatutAvoir.Disponible;
            _audit.Enregistrer("MiseADispositionAvoir", "Avoir", avoir.Id,
                new { Statut = StatutAvoir.Valide.ToString() }, new { Statut = avoir.Statut.ToString() });

            await RegularisationFinanciere.CloturerSiRegulariseeAsync(_context, _audit, avoir.Commande, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task Handle(AnnulerAvoirCommand request, CancellationToken cancellationToken)
        {
            var avoir = await ChargerAsync(request.Id, cancellationToken);
            if (avoir.Statut is StatutAvoir.Utilise or StatutAvoir.Annule)
                throw new ConflictException($"Cet avoir est au statut {avoir.Statut} : il ne peut plus être annulé.");

            var avant = new { Statut = avoir.Statut.ToString(), avoir.Motif };
            avoir.Statut = StatutAvoir.Annule;
            avoir.Motif = RegularisationFinanciere.Tronquer($"{avoir.Motif} | Annulé : {request.Motif.Trim()}");
            _audit.Enregistrer("AnnulationAvoir", "Avoir", avoir.Id, avant, new { Statut = avoir.Statut.ToString(), avoir.Motif });

            await RegularisationFinanciere.CloturerSiRegulariseeAsync(_context, _audit, avoir.Commande, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task<Avoir> ChargerAsync(Guid id, CancellationToken cancellationToken) =>
            await _context.Avoirs.Include(a => a.Commande).FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException("Avoir", id);
    }
}
