using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Finance.Common;
using DigitalAllianceTogo.Application.Stock.Common;
using DigitalAllianceTogo.Domain.Enum;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Commandes.Commands.CloturerCommande
{
    /// <summary>
    /// Clôture (§17) : seulement quand toutes les obligations de la commande sont terminées.
    /// - Livrée → Clôturée (Admin ou Commercial) ; un SAV ultérieur ne la rouvrira pas.
    /// - Tout autre statut : clôture EXCEPTIONNELLE, Administrateur seul, motif obligatoire.
    /// Jamais tant qu'un remboursement est en attente ou échoué, ni tant que du stock
    /// est réservé ou en transit pour la commande.
    /// </summary>
    public record CloturerCommandeCommand : IRequest
    {
        public Guid Id { get; init; }
        public string? Motif { get; init; }
    }

    public class CloturerCommandeCommandValidator : AbstractValidator<CloturerCommandeCommand>
    {
        public CloturerCommandeCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Motif).MaximumLength(500);
        }
    }

    public class CloturerCommandeCommandHandler : IRequestHandler<CloturerCommandeCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditService _audit;

        public CloturerCommandeCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
        {
            _context = context;
            _currentUser = currentUser;
            _audit = audit;
        }

        public async Task Handle(CloturerCommandeCommand request, CancellationToken cancellationToken)
        {
            var commande = await _context.Commandes.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException("Commande", request.Id);

            if (commande.Statut == StatutCommande.Cloturee)
                throw new ConflictException("La commande est déjà clôturée.");

            if (await RegularisationFinanciere.EnCoursAsync(_context, commande.Id, cancellationToken))
                throw new ConflictException("Un remboursement ou un avoir est encore en attente ou a échoué : la commande ne peut pas être clôturée.");

            if (commande.Statut == StatutCommande.Livree)
            {
                RegularisationFinanciere.Cloturer(_audit, commande, request.Motif?.Trim() ?? "Commande livrée");
            }
            else
            {
                if (!_currentUser.EstDansRole(Roles.Admin))
                    throw new ForbiddenAccessException();
                if (string.IsNullOrWhiteSpace(request.Motif))
                    throw new ConflictException("Une clôture exceptionnelle exige un motif.");
                if (commande.Statut is StatutCommande.EnTransit or StatutCommande.LivraisonEchoueeRefusClient or StatutCommande.AnnulationEnCours
                    || await StockCommande.ADuStockReserveAsync(_context, commande.Id, cancellationToken))
                    throw new ConflictException("Des produits de cette commande sont encore réservés, en transit ou à contrôler : régularisez d'abord le stock.");

                RegularisationFinanciere.Cloturer(_audit, commande, $"Clôture exceptionnelle : {request.Motif.Trim()}");
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
