using DigitalAllianceTogo.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Utilisateurs.Queries.GetRoles
{
    /// <summary>Rôles existants : l'Administrateur a besoin de leur Id pour les attribuer.</summary>
    public record GetRolesQuery : IRequest<List<RoleDto>>;

    public record RoleDto(Guid Id, string Nom, string Description);

    public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, List<RoleDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetRolesQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
        {
            return await _context.Roles.AsNoTracking()
                .OrderBy(r => r.Nom)
                .Select(r => new RoleDto(r.Id, r.Nom, r.Description))
                .ToListAsync(cancellationToken);
        }
    }
}
