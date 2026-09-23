using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Finance.Common;
using DigitalAllianceTogo.Application.Stock.Common;
using DigitalAllianceTogo.Domain.Enum;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Commandes.Commands.ReceptionnerRetour
{
    /// <summary>
    /// Le Gestionnaire de stock réceptionne, contrôle et classe les produits (§16, §18) d'une
    /// commande annulée pendant la préparation ou refusée à la livraison :
    /// intacts → de nouveau vendables, endommagés → défectueux.
    /// La commande passe ensuite Annulée, puis En attente de régularisation financière
    /// (ou Clôturée si l'argent est déjà régularisé).
    /// </summary>
    public record ReceptionnerRetourCommand : IRequest<List<object>>
    {
        public Guid CommandeId { get; init; }

        /// <summary>Produits endommagés constatés ; tout ce qui n'est pas listé est intact.</summary>
        public List<ProduitDefectueuxInput> Defectueux { get; init; } = new();
    }

    public record ProduitDefectueuxInput(Guid ProduitId, int Quantite);

    public class ReceptionnerRetourCommandValidator : AbstractValidator<ReceptionnerRetourCommand>
    {
        public ReceptionnerRetourCommandValidator()
        {
            RuleFor(x => x.CommandeId).NotEmpty();
            RuleForEach(x => x.Defectueux).ChildRules(d =>
            {
                d.RuleFor(x => x.ProduitId).NotEmpty();
                d.RuleFor(x => x.Quantite).GreaterThan(0);
            });
            RuleFor(x => x.Defectueux)
                .Must(d => d.Select(x => x.ProduitId).Distinct().Count() == d.Count)
                .WithMessage("Chaque produit ne doit apparaître qu'une fois.");
        }
    }

    public class ReceptionnerRetourCommandHandler : IRequestHandler<ReceptionnerRetourCommand, List<object>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAuditService _audit;

        public ReceptionnerRetourCommandHandler(IApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<List<object>> Handle(ReceptionnerRetourCommand request, CancellationToken cancellationToken)
        {
            var commande = await _context.Commandes.FirstOrDefaultAsync(c => c.Id == request.CommandeId, cancellationToken)
                ?? throw new NotFoundException("Commande", request.CommandeId);

            var livraisonRefusee = commande.Statut switch
            {
                StatutCommande.AnnulationEnCours => null,
                StatutCommande.LivraisonEchoueeRefusClient => await _context.Livraisons
                    .Where(l => l.CommandeId == commande.Id && l.Statut == StatutLivraison.Echouee && l.DatePriseEnCharge != null)
                    .OrderByDescending(l => l.DatePriseEnCharge)
                    .FirstAsync(cancellationToken),
                _ => throw new ConflictException($"La commande est au statut {commande.Statut} : aucun retour n'est attendu.")
            };

            var controle = await StockCommande.ReceptionnerRetourAsync(_context, commande, livraisonRefusee,
                request.Defectueux.ToDictionary(d => d.ProduitId, d => d.Quantite), cancellationToken);

            if (livraisonRefusee is not null)
                livraisonRefusee.Statut = StatutLivraison.Retournee;

            _audit.Enregistrer("ReceptionRetour", "Commande", commande.Id, apres: new
            {
                Origine = livraisonRefusee is null ? "Annulation pendant la préparation" : $"Refus de la livraison {livraisonRefusee.Reference}",
                Controle = controle
            });
            await RegularisationFinanciere.TerminerAnnulationAsync(_context, _audit, commande, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            return controle;
        }
    }
}
