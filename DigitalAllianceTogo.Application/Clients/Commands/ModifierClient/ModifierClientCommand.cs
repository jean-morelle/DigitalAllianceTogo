using DigitalAllianceTogo.Application.Clients.Common;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Enum;
using MediatR;

namespace DigitalAllianceTogo.Application.Clients.Commands.ModifierClient
{
    /// <summary>
    /// Mise à jour de la fiche client (par le personnel, ou par le client lui-même).
    /// - Client avec compte : l'email est celui de connexion, il n'est pas modifiable ici ;
    ///   nom/prénom/téléphone sont répercutés sur le compte.
    /// - La source d'acquisition n'est modifiable que par le personnel.
    /// </summary>
    public record ModifierClientCommand : IRequest, IInfosClient
    {
        public Guid Id { get; init; }
        public TypeClient Type { get; init; } = TypeClient.Particulier;
        public string Nom { get; init; } = string.Empty;
        public string? Prenom { get; init; }
        public string? RaisonSociale { get; init; }
        public string Telephone { get; init; } = string.Empty;
        public string? Email { get; init; }
        public SourceClient? Source { get; init; }
    }

    public class ModifierClientCommandHandler : IRequestHandler<ModifierClientCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditService _audit;

        public ModifierClientCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
        {
            _context = context;
            _currentUser = currentUser;
            _audit = audit;
        }

        public async Task Handle(ModifierClientCommand request, CancellationToken cancellationToken)
        {
            var client = await ClientHelper.ChargerAvecControleAccesAsync(_context, _currentUser, request.Id, cancellationToken);
            // Pas de contrôle de doublon sur le téléphone ici : une inscription peut légitimement
            // partager le numéro d'une fiche prospect (fusion prévue via vérification SMS/WhatsApp).
            var telephone = request.Telephone.Trim();

            var avant = ClientHelper.Instantane(client);

            client.Type = request.Type;
            client.Nom = request.Nom.Trim();
            client.Prenom = request.Prenom?.Trim();
            client.RaisonSociale = request.RaisonSociale?.Trim();
            client.Telephone = telephone;

            if (client.Utilisateur is null)
                client.Email = ClientHelper.NormaliserEmail(request.Email);
            else
            {
                client.Utilisateur.Nom = client.Nom;
                client.Utilisateur.Prenom = client.Prenom ?? string.Empty;
                client.Utilisateur.Telephone = client.Telephone;
            }

            if (request.Source.HasValue && ClientHelper.EstPersonnel(_currentUser))
                client.Source = request.Source.Value;

            _audit.Enregistrer("ModificationClient", "Client", client.Id, avant, ClientHelper.Instantane(client));
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
