using DigitalAllianceTogo.Application.Commandes.Common;
using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Finance.Common;
using DigitalAllianceTogo.Application.Stock.Common;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Finance;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Paiements.Commands.PayerAvecAvoir
{
    /// <summary>
    /// Le client règle tout ou partie d'une commande avec un de ses avoirs disponibles (§23).
    /// Paiement confirmé immédiatement (l'avoir a déjà été validé par l'Administrateur).
    /// - L'avoir couvre tout : commande PaiementConfirme puis réservation du stock.
    /// - Il ne couvre qu'une partie : le client paie le reste par Mobile Money / virement.
    /// - Il dépasse le reste à payer : seul le nécessaire est utilisé, le solde reste disponible.
    /// </summary>
    public record PayerAvecAvoirCommand : IRequest<PayerAvecAvoirResult>
    {
        public Guid CommandeId { get; init; }
        public Guid AvoirId { get; init; }
    }

    public record PayerAvecAvoirResult(decimal MontantUtilise, decimal ResteAPayer, decimal SoldeAvoir, string StatutCommande);

    public class PayerAvecAvoirCommandValidator : AbstractValidator<PayerAvecAvoirCommand>
    {
        public PayerAvecAvoirCommandValidator()
        {
            RuleFor(x => x.CommandeId).NotEmpty();
            RuleFor(x => x.AvoirId).NotEmpty();
        }
    }

    public class PayerAvecAvoirCommandHandler : IRequestHandler<PayerAvecAvoirCommand, PayerAvecAvoirResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditService _audit;

        public PayerAvecAvoirCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
        {
            _context = context;
            _currentUser = currentUser;
            _audit = audit;
        }

        public async Task<PayerAvecAvoirResult> Handle(PayerAvecAvoirCommand request, CancellationToken cancellationToken)
        {
            var commande = await CommandeHelper.ChargerAvecControleAccesAsync(_context, _currentUser, request.CommandeId, cancellationToken);
            if (commande.Statut is not (StatutCommande.CommandeCreee or StatutCommande.PaiementEchoue))
                throw new ConflictException($"Une commande au statut {commande.Statut} n'attend pas de paiement.");

            var avoir = await _context.Avoirs
                .Include(a => a.Commande)
                .FirstOrDefaultAsync(a => a.Id == request.AvoirId, cancellationToken)
                ?? throw new NotFoundException("Avoir", request.AvoirId);

            // Un avoir n'appartient qu'au client de la commande qui l'a fait naître
            if (avoir.Commande.ClientId != commande.ClientId)
                throw new ForbiddenAccessException();
            if (avoir.Statut != StatutAvoir.Disponible || avoir.MontantRestant <= 0)
                throw new ConflictException($"Cet avoir n'est pas utilisable (statut {avoir.Statut}, solde {avoir.MontantRestant:N0} FCFA).");

            var reste = await SoldeCommande.ResteAPayerAsync(_context, commande, cancellationToken);
            if (reste <= 0)
                throw new ConflictException("Il ne reste rien à payer sur cette commande.");

            var utilise = Math.Min(reste, avoir.MontantRestant);
            var versionId = await _context.VersionsCommande
                .Where(v => v.CommandeId == commande.Id && v.NumeroVersion == commande.VersionActive)
                .Select(v => v.Id)
                .FirstAsync(cancellationToken);

            var paiement = new Paiement
            {
                Id = Guid.NewGuid(),
                Reference = DigitalAllianceTogo.Application.Common.References.Generer("PAY"),
                Montant = utilise,
                DatePaiement = DateTime.UtcNow,
                Statut = StatutPaiement.Confirme,
                Mode = ModePaiement.Avoir,
                ReferenceExterne = avoir.Reference,
                AvoirId = avoir.Id,
                ConfirmeParId = _currentUser.UtilisateurId,
                DateConfirmation = DateTime.UtcNow,
                CommandeId = commande.Id,
                VersionCommandeId = versionId
            };
            _context.Paiements.Add(paiement);

            var avoirAvant = new { Statut = avoir.Statut.ToString(), avoir.MontantUtilise };
            avoir.MontantUtilise += utilise;
            avoir.DateUtilisation = DateTime.UtcNow;
            if (avoir.MontantRestant == 0)
                avoir.Statut = StatutAvoir.Utilise;

            _audit.Enregistrer("UtilisationAvoir", "Avoir", avoir.Id, avoirAvant, new
            {
                Statut = avoir.Statut.ToString(),
                avoir.MontantUtilise,
                Solde = avoir.MontantRestant,
                Commande = commande.Reference
            });
            _audit.Enregistrer("PaiementParAvoir", "Paiement", paiement.Id, apres: new
            {
                paiement.Reference,
                paiement.Montant,
                Avoir = avoir.Reference,
                Commande = commande.Reference
            });

            var resteApres = reste - utilise;
            if (resteApres == 0)
            {
                var avant = commande.Statut.ToString();
                commande.Statut = StatutCommande.PaiementConfirme;
                _audit.Enregistrer("PaiementConfirme", "Commande", commande.Id, new { Statut = avant }, new { Statut = commande.Statut.ToString() });
                await ReservationStock.TenterAsync(_context, _audit, commande, cancellationToken);
            }

            // xmin de l'avoir : pas de double dépense ; xmin de la commande si elle change de statut
            await _context.SaveChangesAsync(cancellationToken);

            return new PayerAvecAvoirResult(utilise, resteApres, avoir.MontantRestant, commande.Statut.ToString());
        }
    }
}
