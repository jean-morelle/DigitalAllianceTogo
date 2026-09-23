using DigitalAllianceTogo.Application.Clients.Common;
using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Models.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Clients.Commands.Adresses
{
    /// <summary>
    /// Adresses du client. Supprimer ou modifier une adresse n'affecte jamais
    /// les commandes passées : chaque commande garde sa propre copie (AdresseLivraisonCommande).
    /// </summary>
    public record AdresseInput
    {
        public string Libelle { get; init; } = string.Empty;
        public string Ligne1 { get; init; } = string.Empty;

        /// <summary>Complément / point de repère (très utile pour les livraisons à Lomé).</summary>
        public string? Ligne2 { get; init; }

        public string Ville { get; init; } = string.Empty;
        public string Pays { get; init; } = "Togo";
        public string? CodePostal { get; init; }
    }

    public record AjouterAdresseCommand : AdresseInput, IRequest<Guid>
    {
        public Guid ClientId { get; init; }
    }

    public record ModifierAdresseCommand : AdresseInput, IRequest
    {
        public Guid ClientId { get; init; }
        public Guid AdresseId { get; init; }
    }

    public record SupprimerAdresseCommand(Guid ClientId, Guid AdresseId) : IRequest;

    public class AdresseCommandsHandler :
        IRequestHandler<AjouterAdresseCommand, Guid>,
        IRequestHandler<ModifierAdresseCommand>,
        IRequestHandler<SupprimerAdresseCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public AdresseCommandsHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Guid> Handle(AjouterAdresseCommand request, CancellationToken cancellationToken)
        {
            await ClientHelper.ChargerAvecControleAccesAsync(_context, _currentUser, request.ClientId, cancellationToken);

            var adresse = new Adresse { Id = Guid.NewGuid(), ClientId = request.ClientId };
            Appliquer(adresse, request);

            _context.Adresses.Add(adresse);
            await _context.SaveChangesAsync(cancellationToken);
            return adresse.Id;
        }

        public async Task Handle(ModifierAdresseCommand request, CancellationToken cancellationToken)
        {
            var adresse = await ChargerAsync(request.ClientId, request.AdresseId, cancellationToken);
            Appliquer(adresse, request);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task Handle(SupprimerAdresseCommand request, CancellationToken cancellationToken)
        {
            var adresse = await ChargerAsync(request.ClientId, request.AdresseId, cancellationToken);
            _context.Adresses.Remove(adresse);
            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task<Adresse> ChargerAsync(Guid clientId, Guid adresseId, CancellationToken cancellationToken)
        {
            await ClientHelper.ChargerAvecControleAccesAsync(_context, _currentUser, clientId, cancellationToken);

            return await _context.Adresses.FirstOrDefaultAsync(a => a.Id == adresseId && a.ClientId == clientId, cancellationToken)
                ?? throw new NotFoundException("Adresse", adresseId);
        }

        private static void Appliquer(Adresse adresse, AdresseInput input)
        {
            adresse.Libelle = input.Libelle.Trim();
            adresse.Ligne1 = input.Ligne1.Trim();
            adresse.Ligne2 = input.Ligne2?.Trim();
            adresse.Ville = input.Ville.Trim();
            adresse.Pays = input.Pays.Trim();
            adresse.CodePostal = input.CodePostal?.Trim() ?? string.Empty; // rarement utilisé au Togo
        }
    }
}
