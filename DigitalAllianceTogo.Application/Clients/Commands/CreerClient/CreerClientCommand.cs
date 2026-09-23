using DigitalAllianceTogo.Application.Clients.Common;
using DigitalAllianceTogo.Application.Common;
using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Clients.Commands.CreerClient
{
    /// <summary>
    /// Le Commercial enregistre un client (§4), sans compte sur le site :
    /// typiquement un prospect venu de WhatsApp, TikTok, Facebook ou en boutique.
    /// </summary>
    public record CreerClientCommand : IRequest<Guid>, IInfosClient
    {
        public TypeClient Type { get; init; } = TypeClient.Particulier;
        public string Nom { get; init; } = string.Empty;
        public string? Prenom { get; init; }
        public string? RaisonSociale { get; init; }
        public string Telephone { get; init; } = string.Empty;
        public string? Email { get; init; }
        public SourceClient Source { get; init; } = SourceClient.Autre;
    }

    public class CreerClientCommandHandler : IRequestHandler<CreerClientCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAuditService _audit;

        public CreerClientCommandHandler(IApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<Guid> Handle(CreerClientCommand request, CancellationToken cancellationToken)
        {
            var telephone = request.Telephone.Trim();

            // Évite les doublons : un prospect WhatsApp est identifié par son numéro
            var existant = await _context.Clients.AsNoTracking()
                .Where(c => c.Telephone == telephone)
                .Select(c => c.CodeClient)
                .FirstOrDefaultAsync(cancellationToken);
            if (existant is not null)
                throw new ConflictException($"Un client avec ce téléphone existe déjà ({existant}).");

            var client = new Client
            {
                Id = Guid.NewGuid(),
                CodeClient = References.Generer("CLI"),
                DateCreation = DateTime.UtcNow,
                Type = request.Type,
                Nom = request.Nom.Trim(),
                Prenom = request.Prenom?.Trim(),
                RaisonSociale = request.RaisonSociale?.Trim(),
                Telephone = telephone,
                Email = ClientHelper.NormaliserEmail(request.Email),
                Source = request.Source
            };

            _context.Clients.Add(client);
            _audit.Enregistrer("CreationClient", "Client", client.Id, apres: ClientHelper.Instantane(client));
            await _context.SaveChangesAsync(cancellationToken);

            return client.Id;
        }
    }
}
