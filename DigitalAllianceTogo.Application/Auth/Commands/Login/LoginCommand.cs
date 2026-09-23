using DigitalAllianceTogo.Application.Auth.Dtos;
using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Auth.Commands.Login
{
    public record LoginCommand : IRequest<LoginResultDto>
    {
        public string Email { get; init; } = string.Empty;
        public string MotDePasse { get; init; } = string.Empty;
    }
    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResultDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public LoginCommandHandler(
            IApplicationDbContext context,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<LoginResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var utilisateur = await _context.Utilisateurs
                .Include(u => u.UtilisateurRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            // Même exception que le mot de passe soit faux ou que l'email n'existe pas :
            // ne jamais révéler laquelle des deux informations était incorrecte.
            if (utilisateur is null || !_passwordHasher.Verify(request.MotDePasse, utilisateur.MotDePasseHash))
                throw new UnauthorizedException();

            if (!utilisateur.Actif)
                throw new UnauthorizedException();

            var roles = utilisateur.UtilisateurRoles.Select(ur => ur.Role.Nom).ToList();

            var (token, expiresAtUtc) = _jwtTokenGenerator.GenerateToken(utilisateur.Id, utilisateur.Email, roles);

            return new LoginResultDto
            {
                Token = token,
                ExpiresAtUtc = expiresAtUtc,
                UtilisateurId = utilisateur.Id,
                Nom = utilisateur.Nom,
                Prenom = utilisateur.Prenom,
                Email = utilisateur.Email,
                Roles = roles
            };
        }
    }
}
