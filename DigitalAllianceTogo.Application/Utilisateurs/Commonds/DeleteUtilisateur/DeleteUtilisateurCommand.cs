using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using MediatR;

namespace DigitalAllianceTogo.Application.Utilisateurs.Commonds.DeleteUtilisateur
{
    public record DeleteUtilisateurCommand(Guid Id) : IRequest;
    public class DeleteUtilisateurCommandHandler : IRequestHandler<DeleteUtilisateurCommand>
    {
        private readonly IApplicationDbContext _context;

        public DeleteUtilisateurCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(DeleteUtilisateurCommand request, CancellationToken cancellationToken)
        {
            var utilisateur = await _context.Utilisateurs.FindAsync(new object[] { request.Id }, cancellationToken)
                ?? throw new NotFoundException(nameof(Domain.Models.Security.Utilisateur), request.Id);

            // Note : la suppression réelle est risquée pour un utilisateur qui a déjà
            // de l'historique (commandes en tant que technicien, JournalAudit...) —
            // les contraintes Restrict posées en base bloqueront la suppression si
            // des données dépendent de lui. Dans un vrai projet pro, on préférerait
            // souvent une désactivation logique (Actif = false) plutôt qu'un DELETE.
            _context.Utilisateurs.Remove(utilisateur);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
