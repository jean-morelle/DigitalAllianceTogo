using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Devis.Common;
using DigitalAllianceTogo.Application.Devis.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Devis.Queries.GetDevisById
{
    public record GetDevisByIdQuery(Guid Id) : IRequest<DevisDetailDto>;

    public class GetDevisByIdQueryHandler : IRequestHandler<GetDevisByIdQuery, DevisDetailDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetDevisByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<DevisDetailDto> Handle(GetDevisByIdQuery request, CancellationToken cancellationToken)
        {
            // Contrôle d'accès (404 si inexistant, 403 si le devis appartient à un autre client)
            var devis = await DevisHelper.ChargerAvecControleAccesAsync(_context, _currentUser, request.Id, cancellationToken);

            var lignes = await _context.LignesDevis.AsNoTracking()
                .Where(l => l.DevisId == devis.Id)
                .OrderBy(l => l.Produit.Nom)
                .Select(l => new LigneDevisDto
                {
                    Id = l.Id,
                    ProduitId = l.ProduitId,
                    ProduitReference = l.Produit.Reference,
                    ProduitNom = l.Produit.Nom,
                    Quantite = l.Quantite,
                    PrixUnitaire = l.PrixUnitaire,
                    Remise = l.Remise,
                    Total = l.Total
                })
                .ToListAsync(cancellationToken);

            var commandeId = await _context.Commandes.AsNoTracking()
                .Where(c => c.DevisOrigineId == devis.Id)
                .Select(c => (Guid?)c.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var estPersonnel = DevisHelper.EstPersonnel(_currentUser);

            return new DevisDetailDto
            {
                Id = devis.Id,
                Reference = devis.Reference,
                Statut = devis.Statut.ToString(),
                DateCreation = devis.DateCreation,
                DateValidite = devis.DateValidite,
                SousTotal = devis.SousTotal,
                Remise = devis.Remise,
                Total = devis.Total,
                TauxRemise = devis.TauxRemise,
                ValideParEntreprise = devis.ValideParId != null,
                ValideParId = devis.ValideParId,
                DateValidation = devis.DateValidation,
                CreeParId = devis.CreeParId,
                ClientId = devis.ClientId,
                CodeClient = devis.Client.CodeClient,
                CommentaireClient = devis.CommentaireClient,
                // Les échanges internes (ex : refus de l'Admin) ne sont pas montrés au client
                CommentaireInterne = estPersonnel ? devis.CommentaireInterne : null,
                CommandeId = commandeId,
                Lignes = lignes
            };
        }
    }
}
