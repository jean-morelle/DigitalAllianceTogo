using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Models.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Utilisateurs.Commonds.CreateUtilisateur
{
    public record CreateUtilisateurCommand : IRequest<Guid>
    {
        public string Nom { get; init; } = string.Empty;
        public string Prenom { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Telephone { get; init; } = string.Empty;
        public string MotDePasse { get; init; } = string.Empty;
    }
    public class CreateUtilisateurCommandHandler : IRequestHandler<CreateUtilisateurCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public CreateUtilisateurCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<Guid> Handle(CreateUtilisateurCommand request, CancellationToken cancellationToken)
        {
            var emailExiste = await _context.Utilisateurs
                .AnyAsync(u => u.Email == request.Email, cancellationToken);

            if (emailExiste)
                throw new ConflictException($"Un utilisateur avec l'email \"{request.Email}\" existe déjà.");

            var utilisateur = new Utilisateur
            {
                Id = Guid.NewGuid(),
                Nom = request.Nom,
                Prenom = request.Prenom,
                Email = request.Email,
                Telephone = request.Telephone,
                MotDePasseHash = _passwordHasher.Hash(request.MotDePasse),
                Actif = true,
                DateCreation = DateTime.UtcNow
            };

            _context.Utilisateurs.Add(utilisateur);
            await _context.SaveChangesAsync(cancellationToken);

            return utilisateur.Id;
        }
    }
}
