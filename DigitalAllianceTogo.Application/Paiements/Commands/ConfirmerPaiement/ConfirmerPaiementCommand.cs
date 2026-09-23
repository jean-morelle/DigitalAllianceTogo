using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Stock.Common;
using DigitalAllianceTogo.Domain.Enum;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Paiements.Commands.ConfirmerPaiement
{
    /// <summary>
    /// Le Commercial a vérifié la transaction : paiement Confirmé (qui / quand tracés),
    /// commande PaiementConfirme, puis réservation IMMÉDIATE du stock (§11) :
    /// StockReserve si tout est disponible, sinon EnAttenteDisponibilite.
    /// </summary>
    public record ConfirmerPaiementCommand(Guid Id) : IRequest<ConfirmerPaiementResult>;

    public record ConfirmerPaiementResult(string StatutCommande, bool StockReserve, string Message);

    public class ConfirmerPaiementCommandHandler : IRequestHandler<ConfirmerPaiementCommand, ConfirmerPaiementResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditService _audit;

        public ConfirmerPaiementCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
        {
            _context = context;
            _currentUser = currentUser;
            _audit = audit;
        }

        public async Task<ConfirmerPaiementResult> Handle(ConfirmerPaiementCommand request, CancellationToken cancellationToken)
        {
            var paiement = await _context.Paiements
                .Include(p => p.Commande)
                .Include(p => p.VersionCommande)
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException("Paiement", request.Id);
            var commande = paiement.Commande;

            if (paiement.Statut != StatutPaiement.EnAttente)
                throw new ConflictException($"Ce paiement est déjà traité (statut {paiement.Statut}).");

            if (commande.Statut != StatutCommande.PaiementEnAttente)
                throw new ConflictException($"La commande est au statut {commande.Statut} : le paiement ne peut plus être confirmé.");

            // Paiements liés à une version : si la commande a changé, ce montant n'est plus le bon
            if (paiement.VersionCommande.NumeroVersion != commande.VersionActive)
                throw new ConflictException("La commande a été modifiée depuis ce paiement : il ne correspond plus à la version active.");

            paiement.Statut = StatutPaiement.Confirme;
            paiement.ConfirmeParId = _currentUser.UtilisateurId;
            paiement.DateConfirmation = DateTime.UtcNow;

            commande.Statut = StatutCommande.PaiementConfirme;

            _audit.Enregistrer("ConfirmationPaiement", "Paiement", paiement.Id,
                new { Statut = StatutPaiement.EnAttente.ToString() },
                new { Statut = paiement.Statut.ToString(), paiement.Montant, paiement.ReferenceExterne, Commande = commande.Reference });
            _audit.Enregistrer("PaiementConfirme", "Commande", commande.Id,
                new { Statut = StatutCommande.PaiementEnAttente.ToString() }, new { Statut = commande.Statut.ToString() });

            var reserve = await ReservationStock.TenterAsync(_context, _audit, commande, cancellationToken);

            // Une seule sauvegarde : paiement + commande + stock, ou rien.
            // xmin (commande et stocks) : pas de double confirmation, pas de survente.
            await _context.SaveChangesAsync(cancellationToken);

            return new ConfirmerPaiementResult(commande.Statut.ToString(), reserve, reserve
                ? "Paiement confirmé : le stock est réservé, la commande peut être préparée."
                : "Paiement confirmé, mais le stock est insuffisant : la commande attend un réapprovisionnement.");
        }
    }
}
