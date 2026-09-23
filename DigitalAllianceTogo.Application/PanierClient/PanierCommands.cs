using DigitalAllianceTogo.Application.Commandes.Common;
using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Devis.Common;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Panier;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DevisEntity = DigitalAllianceTogo.Domain.Models.Devis.Devis;

namespace DigitalAllianceTogo.Application.PanierClient
{
    // ---------- Consultation ----------

    public record LignePanierDto(
        Guid ProduitId, string Reference, string Nom, string? ImageUrl, decimal PrixUnitaire, int Quantite, decimal Total,
        int QuantiteDisponible, bool Actif);

    public record PanierDto(List<LignePanierDto> Lignes, int NombreArticles, decimal Total, bool ToutDisponible);

    /// <summary>
    /// Panier du client connecté (§6) : prix du catalogue à jour, total, et disponibilité
    /// (indicative : le stock n'est réservé qu'après paiement confirmé).
    /// </summary>
    public record GetPanierQuery : IRequest<PanierDto>;

    // ---------- Modifications ----------

    /// <summary>Fixe la quantité d'un produit (0 = le retirer).</summary>
    public record DefinirQuantitePanierCommand(Guid ProduitId, int Quantite) : IRequest<PanierDto>;

    public record ViderPanierCommand : IRequest;

    /// <summary>
    /// « Demander un devis » : le panier devient un devis brouillon (prix du catalogue, sans remise)
    /// que le Commercial complète (remise), valide puis envoie. Adapté aux gros volumes et au B2B.
    /// </summary>
    public record DemanderDevisDepuisPanierCommand : IRequest<Guid>
    {
        public string? Commentaire { get; init; }
    }

    /// <summary>
    /// « Commander » : commande directe au prix du catalogue, sans négociation (§6 : « passer à la
    /// demande de devis ou au paiement »). Elle attend ensuite le paiement, comme toute commande.
    /// </summary>
    public record CommanderPanierCommand : IRequest<Guid>
    {
        public Guid AdresseLivraisonId { get; init; }
        public string? TelephoneContact { get; init; }
    }

    public class DefinirQuantitePanierCommandValidator : AbstractValidator<DefinirQuantitePanierCommand>
    {
        public DefinirQuantitePanierCommandValidator()
        {
            RuleFor(x => x.ProduitId).NotEmpty();
            RuleFor(x => x.Quantite).InclusiveBetween(0, PanierHelper.QuantiteMaximaleParLigne)
                .WithMessage($"Quantité entre 0 et {PanierHelper.QuantiteMaximaleParLigne} (au-delà, demandez un devis).");
        }
    }

    public class DemanderDevisDepuisPanierCommandValidator : AbstractValidator<DemanderDevisDepuisPanierCommand>
    {
        public DemanderDevisDepuisPanierCommandValidator() => RuleFor(x => x.Commentaire).MaximumLength(1000);
    }

    public class CommanderPanierCommandValidator : AbstractValidator<CommanderPanierCommand>
    {
        public CommanderPanierCommandValidator()
        {
            RuleFor(x => x.AdresseLivraisonId).NotEmpty().WithMessage("Choisissez une adresse de livraison.");
            RuleFor(x => x.TelephoneContact).MaximumLength(20);
        }
    }

    public class PanierHandler :
        IRequestHandler<GetPanierQuery, PanierDto>,
        IRequestHandler<DefinirQuantitePanierCommand, PanierDto>,
        IRequestHandler<ViderPanierCommand>,
        IRequestHandler<DemanderDevisDepuisPanierCommand, Guid>,
        IRequestHandler<CommanderPanierCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditService _audit;

        public PanierHandler(IApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
        {
            _context = context;
            _currentUser = currentUser;
            _audit = audit;
        }

        public Task<PanierDto> Handle(GetPanierQuery request, CancellationToken cancellationToken) =>
            ConstruireDtoAsync(PanierHelper.UtilisateurConnecte(_currentUser), cancellationToken);

        public async Task<PanierDto> Handle(DefinirQuantitePanierCommand request, CancellationToken cancellationToken)
        {
            var utilisateurId = PanierHelper.UtilisateurConnecte(_currentUser);
            var panier = await PanierHelper.ChargerOuCreerAsync(_context, utilisateurId, cancellationToken);
            var ligne = panier.Lignes.FirstOrDefault(l => l.ProduitId == request.ProduitId);

            if (request.Quantite == 0)
            {
                if (ligne is not null)
                    _context.LignesPanier.Remove(ligne);
            }
            else
            {
                var produit = await _context.Produits.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.ProduitId, cancellationToken)
                    ?? throw new NotFoundException("Produit", request.ProduitId);
                if (!produit.Actif)
                    throw new ConflictException($"Le produit \"{produit.Nom}\" n'est plus disponible à la vente.");

                if (ligne is null)
                {
                    _context.LignesPanier.Add(new LignePanier
                    {
                        Id = Guid.NewGuid(), PanierId = panier.Id, ProduitId = produit.Id, Quantite = request.Quantite, PrixUnitaire = produit.Prix
                    });
                }
                else
                {
                    ligne.Quantite = request.Quantite;
                    ligne.PrixUnitaire = produit.Prix;
                }
            }

            panier.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return await ConstruireDtoAsync(utilisateurId, cancellationToken);
        }

        public async Task Handle(ViderPanierCommand request, CancellationToken cancellationToken)
        {
            var panier = await PanierHelper.ChargerOuCreerAsync(_context, PanierHelper.UtilisateurConnecte(_currentUser), cancellationToken);
            _context.LignesPanier.RemoveRange(panier.Lignes);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<Guid> Handle(DemanderDevisDepuisPanierCommand request, CancellationToken cancellationToken)
        {
            var utilisateurId = PanierHelper.UtilisateurConnecte(_currentUser);
            var clientId = await PanierHelper.ClientConnecteAsync(_context, utilisateurId, cancellationToken);
            var panier = await PanierNonVideAsync(utilisateurId, cancellationToken);
            var parametres = await _context.ParametresEntreprise.AsNoTracking().FirstAsync(cancellationToken);

            var devis = new DevisEntity
            {
                Id = Guid.NewGuid(),
                Reference = DigitalAllianceTogo.Application.Common.References.Generer("DEV"),
                ClientId = clientId,
                Statut = StatutDevis.Brouillon,
                DateCreation = DateTime.UtcNow,
                DateValidite = DateTime.UtcNow.AddDays(parametres.DureeValiditeDevisJours),
                CreeParId = utilisateurId, // demande du client : repérée par le Commercial dans ses tâches
                CommentaireClient = string.IsNullOrWhiteSpace(request.Commentaire) ? null : request.Commentaire.Trim()
            };
            // Mêmes règles qu'un devis du Commercial : prix du catalogue, produits actifs
            await DevisHelper.AppliquerLignesAsync(_context, devis,
                panier.Lignes.Select(l => new LigneDevisInput { ProduitId = l.ProduitId, Quantite = l.Quantite, Remise = 0 }).ToList(),
                0, cancellationToken);

            _context.Devis.Add(devis);
            _context.LignesPanier.RemoveRange(panier.Lignes);
            _audit.Enregistrer("DemandeDevisClient", "Devis", devis.Id, apres: DevisHelper.Instantane(devis));

            await _context.SaveChangesAsync(cancellationToken);
            return devis.Id;
        }

        public async Task<Guid> Handle(CommanderPanierCommand request, CancellationToken cancellationToken)
        {
            var utilisateurId = PanierHelper.UtilisateurConnecte(_currentUser);
            var clientId = await PanierHelper.ClientConnecteAsync(_context, utilisateurId, cancellationToken);
            var panier = await PanierNonVideAsync(utilisateurId, cancellationToken);
            var (adresse, telephone) = await CreationCommande.ResoudreLivraisonAsync(
                _context, clientId, request.AdresseLivraisonId, request.TelephoneContact, cancellationToken);

            // Prix du catalogue au moment de la commande (jamais le prix stocké dans le panier)
            var lignes = new List<CreationCommande.LigneNouvelle>();
            foreach (var ligne in panier.Lignes)
            {
                if (!ligne.Produit.Actif)
                    throw new ConflictException($"Le produit \"{ligne.Produit.Nom}\" n'est plus disponible : retirez-le du panier.");
                lignes.Add(new CreationCommande.LigneNouvelle(ligne.ProduitId, ligne.Quantite, ligne.Produit.Prix, 0, ligne.Produit.Prix * ligne.Quantite));
            }
            var total = lignes.Sum(l => l.Total);

            var commande = CreationCommande.Construire(clientId, adresse, telephone, lignes, total, 0, total, "Commande depuis le panier", devisOrigineId: null);
            _context.Commandes.Add(commande);
            _context.LignesPanier.RemoveRange(panier.Lignes);
            _audit.Enregistrer("CreationCommande", "Commande", commande.Id, apres: new
            {
                commande.Reference,
                Statut = commande.Statut.ToString(),
                Origine = "Panier",
                Version = 1,
                Total = total
            });

            await _context.SaveChangesAsync(cancellationToken);
            return commande.Id;
        }

        private async Task<Domain.Models.Panier.Panier> PanierNonVideAsync(Guid utilisateurId, CancellationToken cancellationToken)
        {
            var panier = await PanierHelper.ChargerOuCreerAsync(_context, utilisateurId, cancellationToken);
            if (panier.Lignes.Count == 0)
                throw new ConflictException("Votre panier est vide.");
            return panier;
        }

        private async Task<PanierDto> ConstruireDtoAsync(Guid utilisateurId, CancellationToken cancellationToken)
        {
            var lignes = await _context.LignesPanier.AsNoTracking()
                .Where(l => l.Panier.UtilisateurId == utilisateurId && l.Panier.Actif)
                .OrderBy(l => l.DateAjout)
                .Select(l => new LignePanierDto(
                    l.ProduitId,
                    l.Produit.Reference,
                    l.Produit.Nom,
                    l.Produit.Images.Where(i => i.EstPrincipale).Select(i => i.Url).FirstOrDefault(),
                    l.Produit.Prix,
                    l.Quantite,
                    l.Produit.Prix * l.Quantite,
                    l.Produit.Stocks.Where(s => s.Entrepot.Actif).Sum(s => s.QuantitePhysique - s.QuantiteReservee),
                    l.Produit.Actif))
                .ToListAsync(cancellationToken);

            return new PanierDto(lignes, lignes.Sum(l => l.Quantite), lignes.Sum(l => l.Total),
                lignes.All(l => l.Actif && l.QuantiteDisponible >= l.Quantite));
        }
    }
}
