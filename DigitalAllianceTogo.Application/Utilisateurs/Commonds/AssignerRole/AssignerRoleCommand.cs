using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Models.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Utilisateurs.Commonds.AssignerRole
{
    public record AssignerRoleCommand(Guid UtilisateurId, Guid RoleId) : IRequest;

    public class AssignerRoleCommandHandler : IRequestHandler<AssignerRoleCommand>
    {
        private readonly IApplicationDbContext _context;

        public AssignerRoleCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(AssignerRoleCommand request, CancellationToken cancellationToken)
        {
            var utilisateurExiste = await _context.Utilisateurs
                .AnyAsync(u => u.Id == request.UtilisateurId, cancellationToken);
            if (!utilisateurExiste)
                throw new NotFoundException(nameof(Utilisateur), request.UtilisateurId);

            var roleExiste = await _context.Roles
                .AnyAsync(r => r.Id == request.RoleId, cancellationToken);
            if (!roleExiste)
                throw new NotFoundException(nameof(Role), request.RoleId);

            var dejaAssigne = await _context.UtilisateurRoles
                .AnyAsync(ur => ur.UtilisateurId == request.UtilisateurId && ur.RoleId == request.RoleId, cancellationToken);
            if (dejaAssigne)
                throw new ConflictException("Ce rôle est déjà assigné à cet utilisateur.");

            _context.UtilisateurRoles.Add(new UtilisateurRole
            {
                Id = Guid.NewGuid(),
                UtilisateurId = request.UtilisateurId,
                RoleId = request.RoleId,
                DateAffectation = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

