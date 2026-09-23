using DigitalAllianceTogo.Application.Commandes.Common;
using DigitalAllianceTogo.Application.Commandes.Dtos;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Finance.Common;
using DigitalAllianceTogo.Application.Finance.Queries;
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

            var remboursements = await _context.Remboursements.AsNoTracking()
                .Where(r => r.CommandeId == commande.Id)
                .OrderBy(r => r.DateDemande)
                .Select(r => new RemboursementDto
                {
                    Id = r.Id, Reference = r.Reference, Montant = r.Montant, Statut = r.Statut.ToString(), Motif = r.Motif,
                    DateDemande = r.DateDemande, DateExecution = r.DateExecution, ReferenceTransaction = r.ReferenceTransaction,
                    MotifEchec = r.MotifEchec, CommandeId = r.CommandeId, CommandeReference = commande.Reference,
                    NumeroVersion = r.VersionCommande.NumeroVersion
                })
                .ToListAsync(cancellationToken);

            var avoirs = await _context.Avoirs.AsNoTracking()
                .Where(a => a.CommandeId == commande.Id)
                .OrderBy(a => a.DateCreation)
                .Select(a => new AvoirDto
                {
                    Id = a.Id, Reference = a.Reference, Montant = a.Montant, Statut = a.Statut.ToString(), Motif = a.Motif,
                    DateCreation = a.DateCreation, DateUtilisation = a.DateUtilisation, CommandeId = a.CommandeId,
                    CommandeReference = commande.Reference, NumeroVersion = a.VersionCommande.NumeroVersion
                })
                .ToListAsync(cancellationToken);

            var versions = await _context.VersionsCommande.AsNoTracking()
                .Include(v => v.Lignes).ThenInclude(l => l.Produit)
                .Where(v => v.CommandeId == commande.Id)
                .OrderBy(v => v.NumeroVersion)
                .ToListAsync(cancellationToken);

            var livraisons = await _context.Livraisons.AsNoTracking()
                .Where(l => l.CommandeId == commande.Id)
                .OrderBy(l => l.DatePlanifiee)
                .Select(l => new SuiviLivraisonDto(l.Reference, l.Type.ToString(), l.Statut.ToString(), l.DatePlanifiee, l.DateLivraison, l.MotifEchec))
                .ToListAsync(cancellationToken);

            return new CommandeDetailDto
            {
                Livraisons = livraisons,
                Remboursements = remboursements,
                Avoirs = avoirs,
                ResteAPayer = version.Total - await SoldeCommande.PayeNetAsync(_context, commande.Id, cancellationToken),
                Versions = versions.Select(v => new VersionCommandeDto
                {
                    Id = v.Id,
                    NumeroVersion = v.NumeroVersion,
                    Statut = v.Statut.ToString(),
                    Active = v.NumeroVersion == commande.VersionActive,
                    DateCreation = v.DateCreation,
                    MotifModification = v.MotifModification,
                    MotifRefus = v.MotifRefus,
                    DateReponse = v.DateReponse,
                    SousTotal = v.SousTotal,
                    Remise = v.Remise,
                    Total = v.Total,
                    Lignes = v.Lignes.Select(l => new LigneCommandeDto
                    {
                        Id = l.Id, ProduitId = l.ProduitId, ProduitReference = l.Produit.Reference, ProduitNom = l.Produit.Nom,
                        Quantite = l.Quantite, PrixUnitaire = l.PrixUnitaire, Remise = l.Remise, Total = l.Total
                    }).ToList()
                }).ToList(),
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
