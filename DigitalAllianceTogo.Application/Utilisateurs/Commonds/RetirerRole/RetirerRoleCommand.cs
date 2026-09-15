using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Utilisateurs.Commonds.RetirerRole
{
    public record RetirerRoleCommand(Guid UtilisateurId, Guid RoleId) : IRequest;
    public class RetirerRoleCommandHandler : IRequestHandler<RetirerRoleCommand>
    {
        private readonly IApplicationDbContext _context;

        public RetirerRoleCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(RetirerRoleCommand request, CancellationToken cancellationToken)
        {
            var assignation = await _context.UtilisateurRoles
                .FirstOrDefaultAsync(
                    ur => ur.UtilisateurId == request.UtilisateurId && ur.RoleId == request.RoleId,
                    cancellationToken)
                ?? throw new NotFoundException("UtilisateurRole", $"{request.UtilisateurId}/{request.RoleId}");

            _context.UtilisateurRoles.Remove(assignation);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
