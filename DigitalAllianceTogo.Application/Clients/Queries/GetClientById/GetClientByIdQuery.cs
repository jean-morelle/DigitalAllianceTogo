using DigitalAllianceTogo.Application.Clients.Common;
using DigitalAllianceTogo.Application.Clients.Dtos;
using DigitalAllianceTogo.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Clients.Queries.GetClientById
{
    /// <summary>
    /// Fiche client + adresses + historique. Id null = la fiche du client connecté ("ma fiche").
    /// </summary>
    public record GetClientByIdQuery(Guid? Id) : IRequest<ClientDetailDto>;

    public class GetClientByIdQueryHandler : IRequestHandler<GetClientByIdQuery, ClientDetailDto>
    {
        private const int TailleHistorique = 5;

        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetClientByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<ClientDetailDto> Handle(GetClientByIdQuery request, CancellationToken cancellationToken)
        {
            var clientId = request.Id ?? await ClientHelper.IdClientConnecteAsync(_context, _currentUser, cancellationToken);
            var client = await ClientHelper.ChargerAvecControleAccesAsync(_context, _currentUser, clientId, cancellationToken);

            var adresses = await _context.Adresses.AsNoTracking()
                .Where(a => a.ClientId == clientId)
                .OrderBy(a => a.Libelle)
                .Select(a => new AdresseDto
                {
                    Id = a.Id,
                    Libelle = a.Libelle,
                    Ligne1 = a.Ligne1,
                    Ligne2 = a.Ligne2,
                    Ville = a.Ville,
                    Pays = a.Pays,
                    CodePostal = a.CodePostal
                })
                .ToListAsync(cancellationToken);

            var devis = _context.Devis.AsNoTracking().Where(d => d.ClientId == clientId);
            var commandes = _context.Commandes.AsNoTracking().Where(c => c.ClientId == clientId);

            return new ClientDetailDto
            {
                Id = client.Id,
                CodeClient = client.CodeClient,
                Type = client.Type.ToString(),
                Nom = client.Nom,
                Prenom = client.Prenom,
                RaisonSociale = client.RaisonSociale,
                Telephone = client.Telephone,
                Email = client.Email,
                Source = client.Source.ToString(),
                DateCreation = client.DateCreation,
                ACompte = client.UtilisateurId != null,
                Adresses = adresses,
                NombreDevis = await devis.CountAsync(cancellationToken),
                NombreCommandes = await commandes.CountAsync(cancellationToken),
                DerniersDevis = await devis
                    .OrderByDescending(d => d.DateCreation)
                    .Take(TailleHistorique)
                    .Select(d => new HistoriqueItemDto
                    {
                        Id = d.Id, Reference = d.Reference, Statut = d.Statut.ToString(), Total = d.Total, Date = d.DateCreation
                    })
                    .ToListAsync(cancellationToken),
                DernieresCommandes = await commandes
                    .OrderByDescending(c => c.DateCreation)
                    .Take(TailleHistorique)
                    .Select(c => new HistoriqueItemDto
                    {
                        Id = c.Id,
                        Reference = c.Reference,
                        Statut = c.Statut.ToString(),
                        // Montant de la version active (les anciennes versions restent en historique)
                        Total = c.Versions.Where(v => v.Active).Select(v => v.Total).FirstOrDefault(),
                        Date = c.DateCreation
                    })
                    .ToListAsync(cancellationToken)
            };
        }
    }
}
