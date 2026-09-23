using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Livraisons.Common;
using DigitalAllianceTogo.Application.Stock.Common;
using DigitalAllianceTogo.Domain.Enum;
using MediatR;

namespace DigitalAllianceTogo.Application.Livraisons.Commands.RemettreAuLivreur
{
    /// <summary>
    /// Le colis est remis au livreur : SORTIE DE STOCK (une seule fois par livraison),
    /// livraison et commande passent En transit.
    /// </summary>
    public record RemettreAuLivreurCommand(Guid Id) : IRequest;

    public class RemettreAuLivreurCommandHandler : IRequestHandler<RemettreAuLivreurCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditService _audit;

        public RemettreAuLivreurCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
        {
            _context = context;
            _currentUser = currentUser;
            _audit = audit;
        }

        public async Task Handle(RemettreAuLivreurCommand request, CancellationToken cancellationToken)
        {
            var livraison = await LivraisonHelper.ChargerAvecControleAccesAsync(_context, _currentUser, request.Id, cancellationToken);
            var commande = livraison.Commande;

            if (livraison.Statut != StatutLivraison.Planifiee)
                throw new ConflictException($"La livraison est au statut {livraison.Statut} : le colis a déjà été remis ou la livraison est terminée.");

            if (commande.Statut != StatutCommande.PretePourLivraison)
                throw new ConflictException($"La commande est au statut {commande.Statut} : elle n'est pas prête pour la livraison.");

            if (livraison.LivreurId is null)
                throw new ConflictException("Aucun livreur n'est assigné à cette livraison.");

            var avant = LivraisonHelper.Instantane(livraison);

            var sorties = await StockCommande.SortirAsync(_context, livraison, cancellationToken);

            livraison.Statut = StatutLivraison.EnTransit;
            livraison.DatePriseEnCharge = DateTime.UtcNow;
            commande.Statut = StatutCommande.EnTransit;

            _audit.Enregistrer("RemiseAuLivreur", "Livraison", livraison.Id, avant, new
            {
                Statut = livraison.Statut.ToString(),
                StatutCommande = commande.Statut.ToString(),
                Sorties = sorties
            });

            // xmin de la commande : un double clic ne sort pas le stock deux fois
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
