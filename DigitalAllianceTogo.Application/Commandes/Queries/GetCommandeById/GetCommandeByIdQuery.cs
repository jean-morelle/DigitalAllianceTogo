using DigitalAllianceTogo.Application.Commandes.Common;
using DigitalAllianceTogo.Application.Commandes.Dtos;
using DigitalAllianceTogo.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Commandes.Queries.GetCommandeById
{
    /// <summary>Détail d'une commande : lignes de la version active et historique des paiements.</summary>
    public record GetCommandeByIdQuery(Guid Id) : IRequest<CommandeDetailDto>;

    public class GetCommandeByIdQueryHandler : IRequestHandler<GetCommandeByIdQuery, CommandeDetailDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetCommandeByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<CommandeDetailDto> Handle(GetCommandeByIdQuery request, CancellationToken cancellationToken)
        {
            // Contrôle d'accès + paiements chargés
            var commande = await CommandeHelper.ChargerAvecControleAccesAsync(_context, _currentUser, request.Id, cancellationToken);

            var adresse = await _context.AdressesLivraisonCommande.AsNoTracking()
                .FirstAsync(a => a.CommandeId == commande.Id, cancellationToken);

            var version = await _context.VersionsCommande.AsNoTracking()
                .Include(v => v.Lignes).ThenInclude(l => l.Produit)
                .FirstAsync(v => v.CommandeId == commande.Id && v.NumeroVersion == commande.VersionActive, cancellationToken);

            var numerosVersion = await _context.VersionsCommande.AsNoTracking()
                .Where(v => v.CommandeId == commande.Id)
                .ToDictionaryAsync(v => v.Id, v => v.NumeroVersion, cancellationToken);

            var parametres = await _context.ParametresEntreprise.AsNoTracking().FirstAsync(cancellationToken);
            var debutDelai = CommandeHelper.DebutDelaiPaiement(commande);

            return new CommandeDetailDto
            {
                Id = commande.Id,
                Reference = commande.Reference,
                Statut = commande.Statut.ToString(),
                DateCreation = commande.DateCreation,
                VersionActive = commande.VersionActive,
                SousTotal = version.SousTotal,
                Remise = version.Remise,
                Total = version.Total,
                ClientId = commande.ClientId,
                CodeClient = commande.Client.CodeClient,
                DevisOrigineId = commande.DevisOrigineId,
                DateLimitePaiement = debutDelai?.AddHours(parametres.DelaiExpirationPaiementHeures),
                AdresseLivraison = new AdresseLivraisonCommandeDto
                {
                    Ligne1 = adresse.Ligne1,
                    Ligne2 = adresse.Ligne2,
                    Ville = adresse.Ville,
                    Pays = adresse.Pays,
                    CodePostal = adresse.CodePostal,
                    TelephoneContact = adresse.TelephoneContact
                },
                Lignes = version.Lignes.Select(l => new LigneCommandeDto
                {
                    Id = l.Id,
                    ProduitId = l.ProduitId,
                    ProduitReference = l.Produit.Reference,
                    ProduitNom = l.Produit.Nom,
                    Quantite = l.Quantite,
                    PrixUnitaire = l.PrixUnitaire,
                    Remise = l.Remise,
                    Total = l.Total
                }).ToList(),
                Paiements = commande.Paiements
                    .OrderByDescending(p => p.DatePaiement)
                    .Select(p => new PaiementDto
                    {
                        Id = p.Id,
                        Reference = p.Reference,
                        Montant = p.Montant,
                        Statut = p.Statut.ToString(),
                        Mode = p.Mode.ToString(),
                        DatePaiement = p.DatePaiement,
                        ReferenceExterne = p.ReferenceExterne,
                        PreuveUrl = p.PreuveUrl,
                        DateConfirmation = p.DateConfirmation,
                        MotifRejet = p.MotifRejet,
                        NumeroVersion = numerosVersion.GetValueOrDefault(p.VersionCommandeId),
                        CommandeId = commande.Id,
                        CommandeReference = commande.Reference
                    }).ToList()
            };
        }
    }
}
