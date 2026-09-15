using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using MediatR;

namespace DigitalAllianceTogo.Application.Utilisateurs.Commonds.UpdateUtilisateur
{
    public class UpdateUtilisateurCommand : IRequest
    {
        public Guid Id { get; init; }
        public string Nom { get; init; } = string.Empty;
        public string Prenom { get; init; } = string.Empty;
        public string Telephone { get; init; } = string.Empty;
        public bool Actif { get; init; }
    }
    public class UpdateUtilisateurCommandHandler : IRequestHandler<UpdateUtilisateurCommand>
    {
        private readonly IApplicationDbContext _context;

        public UpdateUtilisateurCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UpdateUtilisateurCommand request, CancellationToken cancellationToken)
        {
            var utilisateur = await _context.Utilisateurs.FindAsync(new object[] { request.Id }, cancellationToken)
                ?? throw new NotFoundException(nameof(Domain.Models.Security.Utilisateur), request.Id);

            utilisateur.Nom = request.Nom;
            utilisateur.Prenom = request.Prenom;
            utilisateur.Telephone = request.Telephone;
            utilisateur.Actif = request.Actif;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
