using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Sav.Common;
using DigitalAllianceTogo.Application.Stock.Common;
using DigitalAllianceTogo.Domain.Enum;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LivraisonEntity = DigitalAllianceTogo.Domain.Models.Livraison.Livraison;

namespace DigitalAllianceTogo.Application.Sav.Commands.Logistique
{
    /// <summary>
    /// Planifie la NOUVELLE livraison du produit de remplacement (§27), de type RemplacementSav
    /// et liée au ticket. Réserve le produit s'il ne l'est pas encore (tout ou rien).
    /// Ensuite : remise au livreur / livrée / échec comme toute livraison.
    /// </summary>
    public record PlanifierLivraisonSavCommand : IRequest<Guid>
    {
        public Guid TicketId { get; init; }
        public Guid LivreurId { get; init; }
        public DateTime DatePlanifiee { get; init; }
    }

    /// <summary>
    /// Le Gestionnaire de stock réceptionne et contrôle l'ancien produit récupéré chez le
    /// client (§27, §29) : réutilisable → disponible, endommagé → défectueux.
    /// Il ne décide pas de la réparation (rôle du Technicien).
    /// </summary>
    public record ReceptionnerAncienProduitCommand : IRequest<object>
    {
        public Guid TicketId { get; init; }
        public Guid EntrepotId { get; init; }
        public bool Defectueux { get; init; }
    }

    public class PlanifierLivraisonSavCommandValidator : AbstractValidator<PlanifierLivraisonSavCommand>
    {
        public PlanifierLivraisonSavCommandValidator()
        {
            RuleFor(x => x.TicketId).NotEmpty();
            RuleFor(x => x.LivreurId).NotEmpty();
            RuleFor(x => x.DatePlanifiee)
                .GreaterThanOrEqualTo(_ => DateTime.UtcNow.Date)
                .WithMessage("La date de livraison ne peut pas être dans le passé.");
        }
    }

    public class ReceptionnerAncienProduitCommandValidator : AbstractValidator<ReceptionnerAncienProduitCommand>
    {
        public ReceptionnerAncienProduitCommandValidator()
        {
            RuleFor(x => x.TicketId).NotEmpty();
            RuleFor(x => x.EntrepotId).NotEmpty();
        }
    }

    public class LogistiqueSavCommandsHandler :
        IRequestHandler<PlanifierLivraisonSavCommand, Guid>,
        IRequestHandler<ReceptionnerAncienProduitCommand, object>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditService _audit;

        public LogistiqueSavCommandsHandler(IApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
        {
            _context = context;
            _currentUser = currentUser;
            _audit = audit;
        }

        public async Task<Guid> Handle(PlanifierLivraisonSavCommand request, CancellationToken cancellationToken)
        {
            var ticket = await SavHelper.ChargerAvecControleAccesAsync(_context, _currentUser, request.TicketId, cancellationToken);
            SavHelper.ExigerStatut(ticket, StatutSav.RemplacementEnCours);

            if (await _context.Livraisons.AnyAsync(l => l.TicketSAVId == ticket.Id
                    && (l.Statut == StatutLivraison.Planifiee || l.Statut == StatutLivraison.EnTransit), cancellationToken))
                throw new ConflictException("Une livraison de remplacement est déjà en cours pour ce ticket.");

            var estLivreur = await _context.UtilisateurRoles.AnyAsync(ur =>
                ur.UtilisateurId == request.LivreurId && ur.Role.Nom == Roles.Livreur && ur.Utilisateur.Actif, cancellationToken);
            if (!estLivreur)
                throw new ConflictException("L'utilisateur choisi n'est pas un livreur actif.");

            if (!await StockCommande.ReserverPourSavAsync(_context, ticket, ticket.LigneCommande.ProduitId, cancellationToken))
                throw new ConflictException("Le produit de remplacement est en rupture de stock : réapprovisionnez ou changez la décision.");

            var commandeId = await _context.VersionsCommande
                .Where(v => v.Id == ticket.LigneCommande.VersionCommandeId)
                .Select(v => v.CommandeId)
                .FirstAsync(cancellationToken);

            var livraison = new LivraisonEntity
            {
                Id = Guid.NewGuid(),
                Reference = DigitalAllianceTogo.Application.Common.References.Generer("LIV"),
                Type = TypeLivraison.RemplacementSav,
                Statut = StatutLivraison.Planifiee,
                DatePlanifiee = request.DatePlanifiee.Kind == DateTimeKind.Local
                    ? request.DatePlanifiee.ToUniversalTime()
                    : DateTime.SpecifyKind(request.DatePlanifiee, DateTimeKind.Utc),
                CommandeId = commandeId, // pour l'adresse et l'historique ; la commande n'est pas modifiée
                TicketSAVId = ticket.Id,
                LivreurId = request.LivreurId
            };
            _context.Livraisons.Add(livraison);
            _audit.Enregistrer("PlanificationLivraisonSav", "Livraison", livraison.Id, apres: new
            {
                livraison.Reference,
                TicketSAV = ticket.Reference,
                livraison.LivreurId,
                livraison.DatePlanifiee
            });

            await _context.SaveChangesAsync(cancellationToken);
            return livraison.Id;
        }

        public async Task<object> Handle(ReceptionnerAncienProduitCommand request, CancellationToken cancellationToken)
        {
            var ticket = await SavHelper.ChargerAvecControleAccesAsync(_context, _currentUser, request.TicketId, cancellationToken);

            // Réparé : le client garde son produit. Sans décision : le technicien l'a encore en main.
            if (ticket.Decision is null)
                throw new ConflictException("Aucun ancien produit n'est à récupérer : pas de remplacement, remboursement ou avoir décidé.");
            if (ticket.AncienProduitReceptionne)
                throw new ConflictException("L'ancien produit de ce ticket a déjà été réceptionné.");

            var entrepot = await _context.Entrepots.AsNoTracking().FirstOrDefaultAsync(e => e.Id == request.EntrepotId, cancellationToken)
                ?? throw new NotFoundException("Entrepot", request.EntrepotId);
            if (!entrepot.Actif)
                throw new ConflictException($"L'entrepôt \"{entrepot.Nom}\" est désactivé.");

            var controle = await StockCommande.ReceptionnerAncienProduitAsync(
                _context, ticket, ticket.LigneCommande.ProduitId, entrepot.Id, request.Defectueux, cancellationToken);
            ticket.AncienProduitReceptionne = true;
            _audit.Enregistrer("ReceptionAncienProduitSav", "TicketSAV", ticket.Id, apres: controle);

            await _context.SaveChangesAsync(cancellationToken);
            return controle;
        }
    }
}
