using DigitalAllianceTogo.Application.Clients.Common;
using DigitalAllianceTogo.Application.Common;
using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Clients.Commands.InscrireClient
{
    /// <summary>
    /// Inscription libre depuis le site : crée le compte (Utilisateur + rôle Client)
    /// et la fiche Client en une seule opération.
    /// </summary>
    public record InscrireClientCommand : IRequest<Guid>, IInfosClient
    {
        public TypeClient Type { get; init; } = TypeClient.Particulier;
        public string Nom { get; init; } = string.Empty;
        public string? Prenom { get; init; }
        public string? RaisonSociale { get; init; }
        public string Telephone { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string MotDePasse { get; init; } = string.Empty;

        /// <summary>Canal d'origine (ex : lien partagé sur TikTok → ?source=TikTok).</summary>
        public SourceClient Source { get; init; } = SourceClient.SiteWeb;
    }

    public class InscrireClientCommandHandler : IRequestHandler<InscrireClientCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuditService _audit;

        public InscrireClientCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher, IAuditService audit)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _audit = audit;
        }

        public async Task<Guid> Handle(InscrireClientCommand request, CancellationToken cancellationToken)
        {
            var email = ClientHelper.NormaliserEmail(request.Email)!;
            var telephone = request.Telephone.Trim();

            var emailExiste = await _context.Utilisateurs.AnyAsync(u => u.Email.ToLower() == email, cancellationToken);
            if (emailExiste)
                throw new ConflictException("Un compte existe déjà avec cet email.");

            var roleClient = await _context.Roles.FirstOrDefaultAsync(r => r.Nom == Roles.Client, cancellationToken)
                ?? throw new InvalidOperationException("Le rôle Client n'existe pas en base (données initiales non chargées).");

            var utilisateur = new Utilisateur
            {
                Id = Guid.NewGuid(),
                Nom = request.Nom.Trim(),
                Prenom = request.Prenom?.Trim() ?? string.Empty,
                Email = email,
                Telephone = telephone,
                MotDePasseHash = _passwordHasher.Hash(request.MotDePasse),
                Actif = true,
                DateCreation = DateTime.UtcNow
            };
            utilisateur.UtilisateurRoles.Add(new UtilisateurRole
            {
                Id = Guid.NewGuid(),
                RoleId = roleClient.Id,
                DateAffectation = DateTime.UtcNow
            });
            _context.Utilisateurs.Add(utilisateur);

            // Toujours une NOUVELLE fiche : on ne rattache jamais automatiquement une fiche existante
            // sur la seule base du téléphone (n'importe qui pourrait saisir le numéro d'un autre
            // et récupérer son historique). Le rattachement exigera une vérification (code SMS/WhatsApp).
            var client = new Client
            {
                Id = Guid.NewGuid(),
                CodeClient = References.Generer("CLI"),
                DateCreation = DateTime.UtcNow,
                Type = request.Type,
                Nom = utilisateur.Nom,
                Prenom = request.Prenom?.Trim(),
                RaisonSociale = request.RaisonSociale?.Trim(),
                Telephone = telephone,
                Email = email,
                Source = request.Source,
                UtilisateurId = utilisateur.Id
            };
            _context.Clients.Add(client);

            _audit.Enregistrer("InscriptionClient", "Client", client.Id, apres: ClientHelper.Instantane(client), auteurId: utilisateur.Id);
            await _context.SaveChangesAsync(cancellationToken);

            return client.Id;
        }
    }
}
