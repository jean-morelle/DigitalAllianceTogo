using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Finance.Common;
using DigitalAllianceTogo.Application.Livraisons.Common;
using DigitalAllianceTogo.Application.Stock.Common;
using DigitalAllianceTogo.Domain.Enum;
using FluentValidation;
using MediatR;

namespace DigitalAllianceTogo.Application.Livraisons.Commands.SignalerEchecLivraison
{
    /// <summary>
    /// La livraison n'a pas pu se faire.
    /// - Client absent / injoignable : le colis revient au dépôt, reste réservé, la commande
    ///   redevient PretePourLivraison et une relivraison peut être planifiée.
    /// - Refus du client (§16) : la commande passe en LivraisonEchoueeRefusClient. Le colis
    ///   reste « en transit » jusqu'à ce que le Gestionnaire de stock le réceptionne et le
    ///   contrôle (intact / défectueux). La demande de remboursement ou d'avoir est créée
    ///   tout de suite (validation Administrateur).
    /// </summary>
    public record SignalerEchecLivraisonCommand : IRequest
    {
        public Guid Id { get; init; }
        public string Motif { get; init; } = string.Empty;
        public bool RefusClient { get; init; }

        /// <summary>En cas de refus : ce que le client souhaite (remboursement par défaut).</summary>
        public ModeRegularisation Regularisation { get; init; } = ModeRegularisation.Remboursement;
    }

    public class SignalerEchecLivraisonCommandValidator : AbstractValidator<SignalerEchecLivraisonCommand>
    {
        public SignalerEchecLivraisonCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Motif).NotEmpty().WithMessage("Indiquez la raison de l'échec.").MaximumLength(500);
            RuleFor(x => x.Regularisation).IsInEnum();
        }
    }

    public class SignalerEchecLivraisonCommandHandler : IRequestHandler<SignalerEchecLivraisonCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditService _audit;

        public SignalerEchecLivraisonCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
        {
            _context = context;
            _currentUser = currentUser;
            _audit = audit;
        }

        public async Task Handle(SignalerEchecLivraisonCommand request, CancellationToken cancellationToken)
        {
            var livraison = await LivraisonHelper.ChargerAvecControleAccesAsync(_context, _currentUser, request.Id, cancellationToken);
            var commande = livraison.Commande;

            if (livraison.Statut != StatutLivraison.EnTransit)
                throw new ConflictException($"La livraison est au statut {livraison.Statut} : elle n'est pas en cours.");

            var avant = LivraisonHelper.Instantane(livraison);
            livraison.MotifEchec = request.Motif.Trim();

            if (request.RefusClient)
            {
                livraison.Statut = StatutLivraison.Echouee;
                commande.Statut = StatutCommande.LivraisonEchoueeRefusClient;
                await RegularisationFinanciere.CreerAsync(_context, _audit, commande, request.Regularisation,
                    $"Refus de livraison : {livraison.MotifEchec}", cancellationToken);
            }
            else
            {
                await StockCommande.RetournerPourRelivraisonAsync(_context, livraison, cancellationToken);
                livraison.Statut = StatutLivraison.AReprogrammer;
                commande.Statut = StatutCommande.PretePourLivraison;
            }

            _audit.Enregistrer(request.RefusClient ? "RefusLivraison" : "EchecLivraison", "Livraison", livraison.Id,
                avant, LivraisonHelper.Instantane(livraison));
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
