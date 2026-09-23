using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Devis.Common;
using DigitalAllianceTogo.Domain.Enum;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DevisEntity = DigitalAllianceTogo.Domain.Models.Devis.Devis;

namespace DigitalAllianceTogo.Application.Devis.Commands.CreerDevis
{
    /// <summary>Le Commercial crée un devis en brouillon pour un client.</summary>
    public record CreerDevisCommand : IRequest<Guid>
    {
        public Guid ClientId { get; init; }
        public List<LigneDevisInput> Lignes { get; init; } = new();

        /// <summary>Remise globale en montant (FCFA), en plus des remises par ligne.</summary>
        public decimal RemiseGlobale { get; init; }
    }

    public class CreerDevisCommandHandler : IRequestHandler<CreerDevisCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditService _audit;

        public CreerDevisCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
        {
            _context = context;
            _currentUser = currentUser;
            _audit = audit;
        }

        public async Task<Guid> Handle(CreerDevisCommand request, CancellationToken cancellationToken)
        {
            var clientExiste = await _context.Clients.AnyAsync(c => c.Id == request.ClientId, cancellationToken);
            if (!clientExiste)
                throw new NotFoundException("Client", request.ClientId);

            var parametres = await _context.ParametresEntreprise.AsNoTracking().FirstAsync(cancellationToken);

            var devis = new DevisEntity
            {
                Id = Guid.NewGuid(),
                Reference = DevisHelper.GenererReference("DEV"),
                ClientId = request.ClientId,
                Statut = StatutDevis.Brouillon,
                DateCreation = DateTime.UtcNow,
                // Provisoire : recalculée au moment de l'envoi au client
                DateValidite = DateTime.UtcNow.AddDays(parametres.DureeValiditeDevisJours),
                CreeParId = _currentUser.UtilisateurId
            };

            await DevisHelper.AppliquerLignesAsync(_context, devis, request.Lignes, request.RemiseGlobale, cancellationToken);

            _context.Devis.Add(devis);
            _audit.Enregistrer("CreationDevis", "Devis", devis.Id, apres: DevisHelper.Instantane(devis));
            await _context.SaveChangesAsync(cancellationToken);

            return devis.Id;
        }
    }
}
