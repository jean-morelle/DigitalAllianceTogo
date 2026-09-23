using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Devis.Common;
using DigitalAllianceTogo.Domain.Enum;
using MediatR;

namespace DigitalAllianceTogo.Application.Devis.Commands.ModifierDevis
{
    /// <summary>
    /// Le Commercial modifie les lignes / remises d'un devis pas encore envoyé,
    /// ou d'un devis pour lequel le client a demandé une modification.
    /// Toute modification annule la validation interne : il faudra revalider.
    /// </summary>
    public record ModifierDevisCommand : IRequest
    {
        public Guid Id { get; init; }
        public List<LigneDevisInput> Lignes { get; init; } = new();
        public decimal RemiseGlobale { get; init; }
    }

    public class ModifierDevisCommandHandler : IRequestHandler<ModifierDevisCommand>
    {
        private static readonly StatutDevis[] StatutsModifiables =
            { StatutDevis.Brouillon, StatutDevis.ValidationInterne, StatutDevis.ModificationDemandee };

        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditService _audit;

        public ModifierDevisCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
        {
            _context = context;
            _currentUser = currentUser;
            _audit = audit;
        }

        public async Task Handle(ModifierDevisCommand request, CancellationToken cancellationToken)
        {
            var devis = await DevisHelper.ChargerAvecControleAccesAsync(_context, _currentUser, request.Id, cancellationToken, avecLignes: true);

            if (!StatutsModifiables.Contains(devis.Statut))
                throw new ConflictException($"Un devis au statut {devis.Statut} ne peut plus être modifié.");

            var avant = DevisHelper.Instantane(devis);

            await DevisHelper.AppliquerLignesAsync(_context, devis, request.Lignes, request.RemiseGlobale, cancellationToken);

            devis.Statut = StatutDevis.Brouillon;
            devis.ValideParId = null;
            devis.DateValidation = null;

            _audit.Enregistrer("ModificationDevis", "Devis", devis.Id, avant, DevisHelper.Instantane(devis));
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
