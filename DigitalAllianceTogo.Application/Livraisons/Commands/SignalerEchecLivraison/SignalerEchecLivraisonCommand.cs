using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Livraisons.Common;
using DigitalAllianceTogo.Application.Stock.Common;
using DigitalAllianceTogo.Domain.Enum;
using FluentValidation;
using MediatR;

namespace DigitalAllianceTogo.Application.Livraisons.Commands.SignalerEchecLivraison
{
    /// <summary>
    /// La livraison n'a pas pu se faire ; le colis revient au dépôt.
    /// - Client absent / injoignable : la marchandise reste réservée, la commande
    ///   redevient PretePourLivraison et une relivraison peut être planifiée.
    /// - Refus du client : la réservation est libérée, la commande passe en
    ///   LivraisonEchoueeRefusClient (régularisation financière à traiter ensuite).
    /// </summary>
    public record SignalerEchecLivraisonCommand : IRequest
    {
        public Guid Id { get; init; }
        public string Motif { get; init; } = string.Empty;
        public bool RefusClient { get; init; }
    }

    public class SignalerEchecLivraisonCommandValidator : AbstractValidator<SignalerEchecLivraisonCommand>
    {
        public SignalerEchecLivraisonCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Motif).NotEmpty().WithMessage("Indiquez la raison de l'échec.").MaximumLength(500);
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

            await LivraisonStock.RetournerAsync(_context, livraison, garderReservation: !request.RefusClient, cancellationToken);

            livraison.MotifEchec = request.Motif.Trim();
            if (request.RefusClient)
            {
                livraison.Statut = StatutLivraison.Echouee;
                commande.Statut = StatutCommande.LivraisonEchoueeRefusClient;
            }
            else
            {
                livraison.Statut = StatutLivraison.AReprogrammer;
                commande.Statut = StatutCommande.PretePourLivraison;
            }

            _audit.Enregistrer(request.RefusClient ? "RefusLivraison" : "EchecLivraison", "Livraison", livraison.Id,
                avant, LivraisonHelper.Instantane(livraison));
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
