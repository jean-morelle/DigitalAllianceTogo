using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Devis.Common;
using DigitalAllianceTogo.Domain.Enum;
using MediatR;

namespace DigitalAllianceTogo.Application.Devis.Commands.RepondreDevis
{
    public enum ReponseClientDevis
    {
        Refuser,
        DemanderModification
    }

    /// <summary>
    /// Réponse négative du client à un devis envoyé : refus définitif ou demande de modification.
    /// (L'acceptation a sa propre commande car elle crée une commande.)
    /// </summary>
    public record RepondreDevisCommand : IRequest
    {
        public Guid Id { get; init; }
        public ReponseClientDevis Reponse { get; init; }
        public string? Commentaire { get; init; }
    }

    public class RepondreDevisCommandHandler : IRequestHandler<RepondreDevisCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditService _audit;

        public RepondreDevisCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
        {
            _context = context;
            _currentUser = currentUser;
            _audit = audit;
        }

        public async Task Handle(RepondreDevisCommand request, CancellationToken cancellationToken)
        {
            var devis = await DevisHelper.ChargerAvecControleAccesAsync(_context, _currentUser, request.Id, cancellationToken);

            if (DevisHelper.MarquerExpireSiDepasse(devis))
            {
                _audit.Enregistrer("ExpirationDevis", "Devis", devis.Id, apres: DevisHelper.Instantane(devis));
                await _context.SaveChangesAsync(cancellationToken);
                throw new ConflictException("Ce devis a expiré.");
            }

            if (devis.Statut != StatutDevis.Envoye)
                throw new ConflictException($"Un devis au statut {devis.Statut} n'attend pas de réponse du client.");

            var avant = DevisHelper.Instantane(devis);

            devis.Statut = request.Reponse == ReponseClientDevis.Refuser
                ? StatutDevis.Refuse
                : StatutDevis.ModificationDemandee;
            devis.CommentaireClient = request.Commentaire;

            _audit.Enregistrer(
                request.Reponse == ReponseClientDevis.Refuser ? "RefusDevisClient" : "DemandeModificationDevis",
                "Devis", devis.Id, avant, new { Statut = devis.Statut.ToString(), request.Commentaire });
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
