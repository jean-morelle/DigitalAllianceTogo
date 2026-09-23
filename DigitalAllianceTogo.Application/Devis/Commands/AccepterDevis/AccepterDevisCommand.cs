using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Devis.Common;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Application.Commandes.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Devis.Commands.AccepterDevis
{
    /// <summary>
    /// Le client accepte un devis envoyé => création AUTOMATIQUE de la commande (§7-8).
    /// Le devis reste conservé (statut Accepté) comme historique.
    /// La commande ne réserve PAS le stock : cela se fera après confirmation du paiement.
    /// </summary>
    public record AccepterDevisCommand : IRequest<Guid>
    {
        public Guid Id { get; init; }

        /// <summary>Une des adresses du client : elle est copiée (snapshot) dans la commande.</summary>
        public Guid AdresseLivraisonId { get; init; }

        /// <summary>Optionnel : par défaut, le téléphone de la fiche client.</summary>
        public string? TelephoneContact { get; init; }
    }

    public class AccepterDevisCommandHandler : IRequestHandler<AccepterDevisCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditService _audit;

        public AccepterDevisCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
        {
            _context = context;
            _currentUser = currentUser;
            _audit = audit;
        }

        public async Task<Guid> Handle(AccepterDevisCommand request, CancellationToken cancellationToken)
        {
            var devis = await DevisHelper.ChargerAvecControleAccesAsync(_context, _currentUser, request.Id, cancellationToken, avecLignes: true);

            if (DevisHelper.MarquerExpireSiDepasse(devis))
            {
                _audit.Enregistrer("ExpirationDevis", "Devis", devis.Id, apres: DevisHelper.Instantane(devis));
                await _context.SaveChangesAsync(cancellationToken);
                throw new ConflictException("Ce devis a expiré : demandez un nouveau devis à votre commercial.");
            }

            if (devis.Statut != StatutDevis.Envoye)
                throw new ConflictException($"Un devis au statut {devis.Statut} ne peut pas être accepté.");

            var (adresse, telephone) = await CreationCommande.ResoudreLivraisonAsync(
                _context, devis.ClientId, request.AdresseLivraisonId, request.TelephoneContact, cancellationToken);

            var avant = DevisHelper.Instantane(devis);
            devis.Statut = StatutDevis.Accepte;

            // Version 1 = copie exacte du devis accepté (prix figés au moment du devis)
            var commande = CreationCommande.Construire(
                devis.ClientId, adresse, telephone,
                devis.Lignes.Select(l => new CreationCommande.LigneNouvelle(l.ProduitId, l.Quantite, l.PrixUnitaire, l.Remise, l.Total)),
                devis.SousTotal, devis.Remise, devis.Total,
                $"Création depuis le devis {devis.Reference}", devis.Id);

            _context.Commandes.Add(commande);
            _audit.Enregistrer("AcceptationDevis", "Devis", devis.Id, avant, DevisHelper.Instantane(devis));
            _audit.Enregistrer("CreationCommande", "Commande", commande.Id, apres: new
            {
                commande.Reference,
                Statut = commande.Statut.ToString(),
                DevisOrigine = devis.Reference,
                Version = 1,
                devis.Total
            });

            // Une seule sauvegarde : devis accepté + commande créée, ou rien du tout.
            // Le jeton de concurrence du devis empêche une double acceptation simultanée.
            await _context.SaveChangesAsync(cancellationToken);

            return commande.Id;
        }
    }
}
