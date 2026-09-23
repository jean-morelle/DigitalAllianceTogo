using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Livraisons.Queries
{
    /// <summary>
    /// Livreurs actifs, pour planifier une livraison. Le Gestionnaire de stock n'a pas accès
    /// à la gestion des utilisateurs (réservée à l'Admin) : on n'expose ici que le nécessaire,
    /// avec la charge du jour (livraisons planifiées ou en cours).
    /// </summary>
    public record GetLivreursQuery : IRequest<List<LivreurDto>>;

    public record LivreurDto(Guid Id, string Nom, string Telephone, int LivraisonsEnCours);

    public class GetLivreursQueryHandler : IRequestHandler<GetLivreursQuery, List<LivreurDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetLivreursQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<LivreurDto>> Handle(GetLivreursQuery request, CancellationToken cancellationToken)
        {
            return await _context.Utilisateurs.AsNoTracking()
                .Where(u => u.Actif && u.UtilisateurRoles.Any(ur => ur.Role.Nom == Roles.Livreur))
                .OrderBy(u => u.Prenom).ThenBy(u => u.Nom)
                .Select(u => new LivreurDto(
                    u.Id,
                    u.Prenom + " " + u.Nom,
                    u.Telephone,
                    u.LivraisonsEnTantQueLivreur.Count(l => l.Statut == Domain.Enum.StatutLivraison.Planifiee || l.Statut == Domain.Enum.StatutLivraison.EnTransit)))
                .ToListAsync(cancellationToken);
        }
    }
}
