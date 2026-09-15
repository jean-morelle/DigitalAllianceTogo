using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Utilisateurs.Dtos;
using DigitalAllianceTogo.Domain.Models.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Utilisateurs.Queries.GetUtilisateurById
{
    public record GetUtilisateurByIdQuery(Guid Id) : IRequest<UtilisateurDto>;
    public class GetUtilisateurByIdQueryHandler : IRequestHandler<GetUtilisateurByIdQuery, UtilisateurDto>
    {
        private readonly IApplicationDbContext _context;

        public GetUtilisateurByIdQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UtilisateurDto> Handle(GetUtilisateurByIdQuery request, CancellationToken cancellationToken)
        {
            var utilisateur = await _context.Utilisateurs
                .Include(u => u.UtilisateurRoles)
                    .ThenInclude(ur => ur.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException(nameof(Utilisateur), request.Id);

            return UtilisateurDto.FromEntity(utilisateur);
        }
    }
}
