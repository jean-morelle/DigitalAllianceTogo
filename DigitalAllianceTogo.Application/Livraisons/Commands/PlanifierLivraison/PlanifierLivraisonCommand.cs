using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Domain.Enum;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LivraisonEntity = DigitalAllianceTogo.Domain.Models.Livraison.Livraison;

namespace DigitalAllianceTogo.Application.Livraisons.Commands.PlanifierLivraison
{
    /// <summary>
    /// Crée la livraison d'une commande prête et l'assigne à un livreur.
    /// Première livraison = Initial ; après un échec (client absent) = Relivraison.
    /// Le stock ne bouge pas encore : il sort à la remise au livreur.
    /// </summary>
    public record PlanifierLivraisonCommand : IRequest<Guid>
    {
        public Guid CommandeId { get; init; }
        public Guid LivreurId { get; init; }
        public DateTime DatePlanifiee { get; init; }
    }

    public class PlanifierLivraisonCommandValidator : AbstractValidator<PlanifierLivraisonCommand>
    {
        public PlanifierLivraisonCommandValidator()
        {
            RuleFor(x => x.CommandeId).NotEmpty();
            RuleFor(x => x.LivreurId).NotEmpty();
            RuleFor(x => x.DatePlanifiee)
                .GreaterThanOrEqualTo(_ => DateTime.UtcNow.Date)
                .WithMessage("La date de livraison ne peut pas être dans le passé.");
        }
    }

    public class PlanifierLivraisonCommandHandler : IRequestHandler<PlanifierLivraisonCommand, Guid>
    {
        private static readonly StatutLivraison[] StatutsEnCours = { StatutLivraison.Planifiee, StatutLivraison.PriseEnCharge, StatutLivraison.EnTransit };

        private readonly IApplicationDbContext _context;
        private readonly IAuditService _audit;

        public PlanifierLivraisonCommandHandler(IApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<Guid> Handle(PlanifierLivraisonCommand request, CancellationToken cancellationToken)
        {
            var commande = await _context.Commandes
                .Include(c => c.Livraisons)
                .FirstOrDefaultAsync(c => c.Id == request.CommandeId, cancellationToken)
                ?? throw new NotFoundException("Commande", request.CommandeId);

            if (commande.Statut != StatutCommande.PretePourLivraison)
                throw new ConflictException($"La commande est au statut {commande.Statut} : elle doit être prête pour la livraison.");

            if (commande.Livraisons.Any(l => StatutsEnCours.Contains(l.Statut)))
                throw new ConflictException("Une livraison est déjà en cours pour cette commande.");

            var estLivreur = await _context.UtilisateurRoles.AnyAsync(ur =>
                ur.UtilisateurId == request.LivreurId && ur.Role.Nom == Roles.Livreur && ur.Utilisateur.Actif, cancellationToken);
            if (!estLivreur)
                throw new ConflictException("L'utilisateur choisi n'est pas un livreur actif.");

            var livraison = new LivraisonEntity
            {
                Id = Guid.NewGuid(),
                Reference = DigitalAllianceTogo.Application.Common.References.Generer("LIV"),
                Type = commande.Livraisons.Count == 0 ? TypeLivraison.Initial : TypeLivraison.Relivraison,
                Statut = StatutLivraison.Planifiee,
                // PostgreSQL (timestamptz) n'accepte que de l'UTC
                DatePlanifiee = request.DatePlanifiee.Kind == DateTimeKind.Local
                    ? request.DatePlanifiee.ToUniversalTime()
                    : DateTime.SpecifyKind(request.DatePlanifiee, DateTimeKind.Utc),
                CommandeId = commande.Id,
                LivreurId = request.LivreurId
            };
            _context.Livraisons.Add(livraison);

            _audit.Enregistrer("PlanificationLivraison", "Livraison", livraison.Id, apres: new
            {
                livraison.Reference,
                Type = livraison.Type.ToString(),
                Commande = commande.Reference,
                livraison.LivreurId,
                livraison.DatePlanifiee
            });

            await _context.SaveChangesAsync(cancellationToken);
            return livraison.Id;
        }
    }
}
