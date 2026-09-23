using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Devis.Common;
using DigitalAllianceTogo.Domain.Enum;
using MediatR;

namespace DigitalAllianceTogo.Application.Devis.Commands.RefuserValidationDevis
{
    /// <summary>
    /// L'Administrateur refuse la remise exceptionnelle : le devis revient en Brouillon
    /// pour que le Commercial le corrige. (≠ refus du devis par le client.)
    /// </summary>
    public record RefuserValidationDevisCommand : IRequest
    {
        public Guid Id { get; init; }
        public string Motif { get; init; } = string.Empty;
    }

    public class RefuserValidationDevisCommandHandler : IRequestHandler<RefuserValidationDevisCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditService _audit;

        public RefuserValidationDevisCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
        {
            _context = context;
            _currentUser = currentUser;
            _audit = audit;
        }

        public async Task Handle(RefuserValidationDevisCommand request, CancellationToken cancellationToken)
        {
            var devis = await DevisHelper.ChargerAvecControleAccesAsync(_context, _currentUser, request.Id, cancellationToken);

            if (devis.Statut != StatutDevis.ValidationInterne)
                throw new ConflictException("Seul un devis en attente de validation Administrateur peut être refusé.");

            var avant = DevisHelper.Instantane(devis);
            devis.Statut = StatutDevis.Brouillon;
            devis.CommentaireInterne = request.Motif;

            _audit.Enregistrer("RefusValidationDevisAdmin", "Devis", devis.Id, avant,
                new { Statut = devis.Statut.ToString(), request.Motif });
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
