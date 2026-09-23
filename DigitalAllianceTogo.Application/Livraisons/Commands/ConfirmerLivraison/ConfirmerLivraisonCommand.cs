using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Livraisons.Common;
using DigitalAllianceTogo.Application.Stock.Common;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Livraison;
using FluentValidation;
using MediatR;

namespace DigitalAllianceTogo.Application.Livraisons.Commands.ConfirmerLivraison
{
    /// <summary>
    /// Le livreur a remis le colis : preuve obligatoire (photo ou signature, GPS si possible).
    /// Avec une réserve (colis abîmé...), la commande est tout de même livrée :
    /// la réserve est conservée mais n'ouvre PAS de ticket SAV automatiquement.
    /// </summary>
    public record ConfirmerLivraisonCommand : IRequest
    {
        public Guid Id { get; init; }
        public string? PhotoUrl { get; init; }
        public string? SignatureUrl { get; init; }
        public decimal? Latitude { get; init; }
        public decimal? Longitude { get; init; }
        public string? Commentaire { get; init; }

        /// <summary>Anomalie signalée par le client à la réception (livraison « avec réserve »).</summary>
        public string? Reserve { get; init; }
    }

    public class ConfirmerLivraisonCommandValidator : AbstractValidator<ConfirmerLivraisonCommand>
    {
        public ConfirmerLivraisonCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.PhotoUrl) || !string.IsNullOrWhiteSpace(x.SignatureUrl))
                .WithName("Preuve")
                .WithMessage("Une photo ou une signature est obligatoire comme preuve de livraison.");
            RuleFor(x => x.PhotoUrl).MaximumLength(1000).Must(EstUneUrl!).When(x => !string.IsNullOrWhiteSpace(x.PhotoUrl))
                .WithMessage("Le lien de la photo doit être une adresse web valide.");
            RuleFor(x => x.SignatureUrl).MaximumLength(1000).Must(EstUneUrl!).When(x => !string.IsNullOrWhiteSpace(x.SignatureUrl))
                .WithMessage("Le lien de la signature doit être une adresse web valide.");
            RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
            RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
            RuleFor(x => x.Longitude).NotNull().When(x => x.Latitude.HasValue).WithMessage("Latitude et longitude vont ensemble.");
            RuleFor(x => x.Latitude).NotNull().When(x => x.Longitude.HasValue).WithMessage("Latitude et longitude vont ensemble.");
            RuleFor(x => x.Commentaire).MaximumLength(1000);
            RuleFor(x => x.Reserve).MaximumLength(1000);
        }

        private static bool EstUneUrl(string url) =>
            Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }

    public class ConfirmerLivraisonCommandHandler : IRequestHandler<ConfirmerLivraisonCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditService _audit;

        public ConfirmerLivraisonCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
        {
            _context = context;
            _currentUser = currentUser;
            _audit = audit;
        }

        public async Task Handle(ConfirmerLivraisonCommand request, CancellationToken cancellationToken)
        {
            var livraison = await LivraisonHelper.ChargerAvecControleAccesAsync(_context, _currentUser, request.Id, cancellationToken);
            var commande = livraison.Commande;

            if (livraison.Statut != StatutLivraison.EnTransit)
                throw new ConflictException($"La livraison est au statut {livraison.Statut} : elle n'est pas en cours.");

            var avant = LivraisonHelper.Instantane(livraison);
            var reserve = string.IsNullOrWhiteSpace(request.Reserve) ? null : request.Reserve.Trim();

            await StockCommande.LivrerAsync(_context, livraison, cancellationToken);

            livraison.Statut = reserve is null ? StatutLivraison.Livree : StatutLivraison.LivreeAvecReserve;
            livraison.Reserve = reserve;
            livraison.DateLivraison = DateTime.UtcNow;
            _context.PreuvesLivraison.Add(new PreuveLivraison
            {
                Id = Guid.NewGuid(),
                DatePreuve = DateTime.UtcNow,
                PhotoUrl = request.PhotoUrl?.Trim(),
                SignatureUrl = request.SignatureUrl?.Trim(),
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Commentaire = request.Commentaire?.Trim(),
                LivraisonId = livraison.Id
            });

            commande.Statut = StatutCommande.Livree;

            _audit.Enregistrer("ConfirmationLivraison", "Livraison", livraison.Id, avant, LivraisonHelper.Instantane(livraison));
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
