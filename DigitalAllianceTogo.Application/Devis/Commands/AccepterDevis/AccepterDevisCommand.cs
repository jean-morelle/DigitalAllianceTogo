using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Devis.Common;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Commande;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CommandeEntity = DigitalAllianceTogo.Domain.Models.Commande.Commande;

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

        /// <summary>Optionnel : par défaut, le téléphone du compte client.</summary>
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

            var adresse = await _context.Adresses.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == request.AdresseLivraisonId && a.ClientId == devis.ClientId, cancellationToken)
                ?? throw new NotFoundException("Adresse", request.AdresseLivraisonId);

            var telephone = request.TelephoneContact;
            if (string.IsNullOrWhiteSpace(telephone))
            {
                telephone = await _context.Clients
                    .Where(c => c.Id == devis.ClientId)
                    .Select(c => c.Utilisateur != null ? c.Utilisateur.Telephone : null)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            if (string.IsNullOrWhiteSpace(telephone))
                throw new ConflictException("Un téléphone de contact est nécessaire pour la livraison.");

            var avant = DevisHelper.Instantane(devis);
            devis.Statut = StatutDevis.Accepte;

            var commande = new CommandeEntity
            {
                Id = Guid.NewGuid(),
                Reference = DevisHelper.GenererReference("CMD"),
                Statut = StatutCommande.CommandeCreee,
                DateCreation = DateTime.UtcNow,
                ClientId = devis.ClientId,
                DevisOrigineId = devis.Id,
                VersionActive = 1,
                AdresseLivraison = new AdresseLivraisonCommande
                {
                    Id = Guid.NewGuid(),
                    Ligne1 = adresse.Ligne1,
                    Ligne2 = adresse.Ligne2,
                    Ville = adresse.Ville,
                    Pays = adresse.Pays,
                    CodePostal = adresse.CodePostal,
                    TelephoneContact = telephone
                }
            };

            // Version 1 = copie exacte du devis accepté (prix figés au moment du devis)
            var version = new VersionCommande
            {
                Id = Guid.NewGuid(),
                NumeroVersion = 1,
                DateCreation = DateTime.UtcNow,
                MotifModification = $"Création depuis le devis {devis.Reference}",
                SousTotal = devis.SousTotal,
                Remise = devis.Remise,
                Total = devis.Total,
                Active = true
            };
            foreach (var ligne in devis.Lignes)
            {
                version.Lignes.Add(new LigneCommande
                {
                    Id = Guid.NewGuid(),
                    ProduitId = ligne.ProduitId,
                    Quantite = ligne.Quantite,
                    PrixUnitaire = ligne.PrixUnitaire,
                    Remise = ligne.Remise,
                    Total = ligne.Total
                });
            }
            commande.Versions.Add(version);

            _context.Commandes.Add(commande);
            _audit.Enregistrer("AcceptationDevis", "Devis", devis.Id, avant, DevisHelper.Instantane(devis));
            _audit.Enregistrer("CreationCommande", "Commande", commande.Id, apres: new
            {
                commande.Reference,
                Statut = commande.Statut.ToString(),
                DevisOrigine = devis.Reference,
                Version = 1,
                version.Total
            });

            // Une seule sauvegarde : devis accepté + commande créée, ou rien du tout.
            // Le jeton de concurrence du devis empêche une double acceptation simultanée.
            await _context.SaveChangesAsync(cancellationToken);

            return commande.Id;
        }
    }
}
