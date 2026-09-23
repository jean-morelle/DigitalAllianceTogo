using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Enum;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Commandes.Commands.Preparation
{
    /// <summary>Le Gestionnaire de stock commence à préparer une commande dont le stock est réservé.</summary>
    public record DemarrerPreparationCommand(Guid CommandeId) : IRequest;

    /// <summary>Colis prêt : la commande peut être confiée à un livreur.</summary>
    public record TerminerPreparationCommand(Guid CommandeId) : IRequest;

    public class PreparationCommandsHandler : IRequestHandler<DemarrerPreparationCommand>, IRequestHandler<TerminerPreparationCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAuditService _audit;

        public PreparationCommandsHandler(IApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public Task Handle(DemarrerPreparationCommand request, CancellationToken cancellationToken) =>
            ChangerStatutAsync(request.CommandeId, StatutCommande.StockReserve, StatutCommande.PreparationEnCours, "DebutPreparation", cancellationToken);

        public Task Handle(TerminerPreparationCommand request, CancellationToken cancellationToken) =>
            ChangerStatutAsync(request.CommandeId, StatutCommande.PreparationEnCours, StatutCommande.PretePourLivraison, "FinPreparation", cancellationToken);

        private async Task ChangerStatutAsync(Guid commandeId, StatutCommande attendu, StatutCommande nouveau, string action, CancellationToken cancellationToken)
        {
            var commande = await _context.Commandes.FirstOrDefaultAsync(c => c.Id == commandeId, cancellationToken)
                ?? throw new NotFoundException("Commande", commandeId);

            if (commande.Statut != attendu)
                throw new ConflictException($"La commande est au statut {commande.Statut} (attendu : {attendu}).");

            commande.Statut = nouveau;
            _audit.Enregistrer(action, "Commande", commande.Id, new { Statut = attendu.ToString() }, new { Statut = nouveau.ToString() });

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
