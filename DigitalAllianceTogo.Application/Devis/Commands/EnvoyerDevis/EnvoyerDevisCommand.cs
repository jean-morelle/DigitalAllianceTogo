using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Devis.Common;
using DigitalAllianceTogo.Domain.Enum;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Devis.Commands.EnvoyerDevis
{
    /// <summary>
    /// Envoi au client d'un devis validé par l'entreprise.
    /// La date de validité démarre à l'envoi.
    /// </summary>
    public record EnvoyerDevisCommand(Guid Id) : IRequest;

    public class EnvoyerDevisCommandHandler : IRequestHandler<EnvoyerDevisCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditService _audit;

        public EnvoyerDevisCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
        {
            _context = context;
            _currentUser = currentUser;
            _audit = audit;
        }

        public async Task Handle(EnvoyerDevisCommand request, CancellationToken cancellationToken)
        {
            var devis = await DevisHelper.ChargerAvecControleAccesAsync(_context, _currentUser, request.Id, cancellationToken);

            if (devis.Statut is not (StatutDevis.Brouillon or StatutDevis.ValidationInterne))
                throw new ConflictException($"Un devis au statut {devis.Statut} ne peut pas être envoyé.");

            if (devis.ValideParId is null)
                throw new ConflictException("Le devis doit être validé par l'entreprise avant d'être envoyé au client.");

            var parametres = await _context.ParametresEntreprise.AsNoTracking().FirstAsync(cancellationToken);
            var avant = DevisHelper.Instantane(devis);

            devis.Statut = StatutDevis.Envoye;
            devis.DateValidite = DateTime.UtcNow.AddDays(parametres.DureeValiditeDevisJours);
            devis.CommentaireClient = null;

            _audit.Enregistrer("EnvoiDevis", "Devis", devis.Id, avant, DevisHelper.Instantane(devis));
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
