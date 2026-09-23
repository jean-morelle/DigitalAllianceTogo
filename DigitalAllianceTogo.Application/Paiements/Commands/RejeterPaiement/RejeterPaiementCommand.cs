using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Enum;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Paiements.Commands.RejeterPaiement
{
    /// <summary>
    /// La preuve ne correspond à aucune transaction réelle (ou pas au bon montant) :
    /// paiement Échoué, commande PaiementEchoue. Le client peut soumettre une nouvelle
    /// preuve pendant le délai paramétré, après quoi la commande est annulée.
    /// </summary>
    public record RejeterPaiementCommand : IRequest
    {
        public Guid Id { get; init; }
        public string Motif { get; init; } = string.Empty;
    }

    public class RejeterPaiementCommandHandler : IRequestHandler<RejeterPaiementCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditService _audit;

        public RejeterPaiementCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
        {
            _context = context;
            _currentUser = currentUser;
            _audit = audit;
        }

        public async Task Handle(RejeterPaiementCommand request, CancellationToken cancellationToken)
        {
            var paiement = await _context.Paiements
                .Include(p => p.Commande)
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException("Paiement", request.Id);
            var commande = paiement.Commande;

            if (paiement.Statut != StatutPaiement.EnAttente)
                throw new ConflictException($"Ce paiement est déjà traité (statut {paiement.Statut}).");

            if (commande.Statut != StatutCommande.PaiementEnAttente)
                throw new ConflictException($"La commande est au statut {commande.Statut} : le paiement ne peut plus être rejeté.");

            paiement.Statut = StatutPaiement.Echoue;
            paiement.MotifRejet = request.Motif.Trim();
            // ConfirmePar / DateConfirmation = qui a vérifié la preuve et quand, même pour un rejet.
            // La date sert aussi de point de départ au délai de nouvelle tentative.
            paiement.ConfirmeParId = _currentUser.UtilisateurId;
            paiement.DateConfirmation = DateTime.UtcNow;

            commande.Statut = StatutCommande.PaiementEchoue;

            _audit.Enregistrer("RejetPaiement", "Paiement", paiement.Id,
                new { Statut = StatutPaiement.EnAttente.ToString() },
                new { Statut = paiement.Statut.ToString(), paiement.MotifRejet, Commande = commande.Reference });
            _audit.Enregistrer("PaiementEchoue", "Commande", commande.Id,
                new { Statut = StatutCommande.PaiementEnAttente.ToString() }, new { Statut = commande.Statut.ToString() });

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
