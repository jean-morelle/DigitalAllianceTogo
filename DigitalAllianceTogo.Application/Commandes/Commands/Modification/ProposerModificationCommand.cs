using DigitalAllianceTogo.Application.Commandes.Common;
using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Devis.Common;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Commande;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Commandes.Commands.Modification
{
    /// <summary>
    /// Le Commercial propose une modification d'une commande payée (§20) : c'est une NOUVELLE
    /// version, inactive tant que le client ne l'a pas acceptée. La version active reste intacte.
    ///
    /// Prix : un produit déjà commandé garde son prix de la commande ; un nouveau produit prend
    /// le prix du catalogue. Validation Administrateur préalable (§21) si la hausse dépasse le
    /// seuil paramétré ou si la remise dépasse le seuil commercial (sauf si l'Admin propose).
    /// </summary>
    public record ProposerModificationCommand : IRequest<ProposerModificationResult>
    {
        public Guid CommandeId { get; init; }
        public List<LigneDevisInput> Lignes { get; init; } = new();
        public decimal RemiseGlobale { get; init; }
        public string Motif { get; init; } = string.Empty;
    }

    public record ProposerModificationResult(Guid VersionId, int NumeroVersion, string Statut, decimal AncienTotal, decimal NouveauTotal, decimal Ecart, string Message);

    public class ProposerModificationCommandValidator : AbstractValidator<ProposerModificationCommand>
    {
        public ProposerModificationCommandValidator()
        {
            RuleFor(x => x.CommandeId).NotEmpty();
            RuleFor(x => x.Lignes).NotEmpty().WithMessage("La nouvelle version doit contenir au moins un produit.");
            RuleForEach(x => x.Lignes).SetValidator(new LigneDevisInputValidator());
            RuleFor(x => x.Lignes)
                .Must(l => l.Select(x => x.ProduitId).Distinct().Count() == l.Count)
                .WithMessage("Chaque produit ne doit apparaître qu'une fois.");
            RuleFor(x => x.RemiseGlobale).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Motif).NotEmpty().WithMessage("Expliquez la modification (demande du client...).").MaximumLength(1000);
        }
    }

    public class ProposerModificationCommandHandler : IRequestHandler<ProposerModificationCommand, ProposerModificationResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditService _audit;

        public ProposerModificationCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
        {
            _context = context;
            _currentUser = currentUser;
            _audit = audit;
        }

        public async Task<ProposerModificationResult> Handle(ProposerModificationCommand request, CancellationToken cancellationToken)
        {
            var commande = await CommandeHelper.ChargerAvecControleAccesAsync(_context, _currentUser, request.CommandeId, cancellationToken);
            await ModificationHelper.VerifierModifiableAsync(_context, commande, cancellationToken);

            if (await _context.VersionsCommande.AnyAsync(v => v.CommandeId == commande.Id && ModificationHelper.StatutsEnCours.Contains(v.Statut), cancellationToken))
                throw new ConflictException("Une proposition de modification est déjà en cours pour cette commande.");

            var active = await _context.VersionsCommande.AsNoTracking()
                .Include(v => v.Lignes)
                .FirstAsync(v => v.CommandeId == commande.Id && v.NumeroVersion == commande.VersionActive, cancellationToken);

            var produitIds = request.Lignes.Select(l => l.ProduitId).ToList();
            var produits = await _context.Produits.AsNoTracking()
                .Where(p => produitIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            var numero = await _context.VersionsCommande.Where(v => v.CommandeId == commande.Id).MaxAsync(v => v.NumeroVersion, cancellationToken) + 1;
            var version = new VersionCommande
            {
                Id = Guid.NewGuid(),
                NumeroVersion = numero,
                DateCreation = DateTime.UtcNow,
                MotifModification = request.Motif.Trim(),
                Active = false,
                CommandeId = commande.Id,
                CreeParId = _currentUser.UtilisateurId
            };

            decimal sousTotal = 0, remiseLignes = 0;
            foreach (var ligne in request.Lignes)
            {
                if (!produits.TryGetValue(ligne.ProduitId, out var produit))
                    throw new NotFoundException("Produit", ligne.ProduitId);

                // Prix figé à la commande pour un produit déjà présent ; catalogue sinon
                var existante = active.Lignes.FirstOrDefault(l => l.ProduitId == ligne.ProduitId);
                if (existante is null && !produit.Actif)
                    throw new ConflictException($"Le produit \"{produit.Nom}\" n'est plus disponible à la vente.");
                var prix = existante?.PrixUnitaire ?? produit.Prix;

                var brut = prix * ligne.Quantite;
                if (ligne.Remise > brut)
                    throw new ConflictException($"La remise sur \"{produit.Nom}\" dépasse le montant de la ligne.");

                version.Lignes.Add(new LigneCommande
                {
                    Id = Guid.NewGuid(),
                    ProduitId = produit.Id,
                    Quantite = ligne.Quantite,
                    PrixUnitaire = prix,
                    Remise = ligne.Remise,
                    Total = brut - ligne.Remise
                });
                sousTotal += brut;
                remiseLignes += ligne.Remise;
            }

            if (remiseLignes + request.RemiseGlobale > sousTotal)
                throw new ConflictException("La remise totale dépasse le montant de la commande.");

            version.SousTotal = sousTotal;
            version.Remise = remiseLignes + request.RemiseGlobale;
            version.Total = sousTotal - version.Remise;

            var identique = version.Total == active.Total
                && version.Lignes.Count == active.Lignes.Count
                && version.Lignes.All(n => active.Lignes.Any(a => a.ProduitId == n.ProduitId && a.Quantite == n.Quantite && a.Remise == n.Remise));
            if (identique)
                throw new ConflictException("La nouvelle version est identique à la version active.");

            // Validation Administrateur (§21) : hausse au-delà du seuil, ou remise exceptionnelle
            var parametres = await _context.ParametresEntreprise.AsNoTracking().FirstAsync(cancellationToken);
            var ecart = version.Total - active.Total;
            var haussePourcent = active.Total == 0 ? 100 : ecart * 100 / active.Total;
            var tauxRemise = sousTotal == 0 ? 0 : version.Remise * 100 / sousTotal;
            var raisonsAdmin = new List<string>();
            if (ecart > 0 && haussePourcent > parametres.SeuilAugmentationModificationPourcent)
                raisonsAdmin.Add($"hausse de {haussePourcent:0.##} % (seuil {parametres.SeuilAugmentationModificationPourcent} %)");
            if (tauxRemise > parametres.SeuilRemiseCommercialPourcent)
                raisonsAdmin.Add($"remise de {tauxRemise:0.##} % (seuil {parametres.SeuilRemiseCommercialPourcent} %)");

            var estAdmin = _currentUser.EstDansRole(Roles.Admin);
            if (raisonsAdmin.Count > 0 && !estAdmin)
            {
                version.Statut = StatutVersionCommande.EnValidationAdmin;
            }
            else
            {
                version.Statut = StatutVersionCommande.EnAttenteClient;
                if (raisonsAdmin.Count > 0)
                    version.ValideParId = _currentUser.UtilisateurId; // proposée par l'Admin lui-même
            }

            _context.VersionsCommande.Add(version);
            _audit.Enregistrer("PropositionModificationCommande", "Commande", commande.Id,
                new { VersionActive = commande.VersionActive, active.Total },
                new { Proposition = ModificationHelper.Instantane(version), Ecart = ecart, ValidationAdmin = raisonsAdmin });

            await _context.SaveChangesAsync(cancellationToken);

            var message = version.Statut == StatutVersionCommande.EnValidationAdmin
                ? $"Proposition v{numero} soumise à l'Administrateur : {string.Join(" et ", raisonsAdmin)}."
                : $"Proposition v{numero} envoyée au client pour acceptation.";
            return new ProposerModificationResult(version.Id, numero, version.Statut.ToString(), active.Total, version.Total, ecart, message);
        }
    }
}
