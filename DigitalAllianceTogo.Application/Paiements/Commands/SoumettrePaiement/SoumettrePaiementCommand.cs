using DigitalAllianceTogo.Application.Commandes.Common;
using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Finance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Paiements.Commands.SoumettrePaiement
{
    /// <summary>
    /// Paiement EXTERNE (§9) : le client a payé par Mobile Money / virement et fournit
    /// sa preuve. Le paiement reste EnAttente jusqu'à la vérification par le Commercial.
    /// Le montant est TOUJOURS celui de la version active : pas de paiement partiel.
    /// </summary>
    public record SoumettrePaiementCommand : IRequest<Guid>
    {
        public Guid CommandeId { get; init; }

        /// <summary>Identifiant de la transaction (ex : référence T-Money / Flooz).</summary>
        public string ReferenceExterne { get; init; } = string.Empty;

        /// <summary>Lien vers la capture ou le reçu (optionnel).</summary>
        public string? PreuveUrl { get; init; }
    }

    public class SoumettrePaiementCommandHandler : IRequestHandler<SoumettrePaiementCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditService _audit;

        public SoumettrePaiementCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
        {
            _context = context;
            _currentUser = currentUser;
            _audit = audit;
        }

        public async Task<Guid> Handle(SoumettrePaiementCommand request, CancellationToken cancellationToken)
        {
            var commande = await CommandeHelper.ChargerAvecControleAccesAsync(_context, _currentUser, request.CommandeId, cancellationToken);

            var parametres = await _context.ParametresEntreprise.AsNoTracking().FirstAsync(cancellationToken);
            if (CommandeHelper.AnnulerSiDelaiDepasse(commande, parametres.DelaiExpirationPaiementHeures, DateTime.UtcNow))
            {
                _audit.Enregistrer("ExpirationCommande", "Commande", commande.Id, apres: new { Statut = commande.Statut.ToString() });
                await _context.SaveChangesAsync(cancellationToken);
                throw new ConflictException("Le délai de paiement est dépassé : la commande a été annulée.");
            }

            if (commande.Statut is not (StatutCommande.CommandeCreee or StatutCommande.PaiementEchoue))
                throw new ConflictException($"Une commande au statut {commande.Statut} n'attend pas de paiement.");

            // Une même transaction ne doit pas payer deux commandes (ni deux fois la même)
            var referenceExterne = request.ReferenceExterne.Trim().ToUpperInvariant();
            if (await _context.Paiements.AnyAsync(p => p.ReferenceExterne == referenceExterne && p.Statut != StatutPaiement.Echoue, cancellationToken))
                throw new ConflictException("Cette référence de transaction a déjà été utilisée.");

            var version = await _context.VersionsCommande.AsNoTracking()
                .FirstAsync(v => v.CommandeId == commande.Id && v.NumeroVersion == commande.VersionActive, cancellationToken);

            var paiement = new Paiement
            {
                Id = Guid.NewGuid(),
                Reference = DigitalAllianceTogo.Application.Common.References.Generer("PAY"),
                Montant = version.Total,
                DatePaiement = DateTime.UtcNow,
                Statut = StatutPaiement.EnAttente,
                Mode = ModePaiement.Externe,
                ReferenceExterne = referenceExterne,
                PreuveUrl = request.PreuveUrl?.Trim(),
                CommandeId = commande.Id,
                VersionCommandeId = version.Id
            };
            _context.Paiements.Add(paiement);

            var avant = commande.Statut.ToString();
            commande.Statut = StatutCommande.PaiementEnAttente;

            _audit.Enregistrer("SoumissionPaiement", "Paiement", paiement.Id, apres: new
            {
                paiement.Reference,
                paiement.Montant,
                paiement.ReferenceExterne,
                Commande = commande.Reference,
                Version = version.NumeroVersion
            });
            _audit.Enregistrer("PaiementEnAttente", "Commande", commande.Id,
                new { Statut = avant }, new { Statut = commande.Statut.ToString() });

            // Le jeton xmin de la commande empêche deux preuves soumises en même temps
            await _context.SaveChangesAsync(cancellationToken);

            return paiement.Id;
        }
    }
}
