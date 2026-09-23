using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using FluentValidation;
using MediatR;
using ValidationException = DigitalAllianceTogo.Application.Common.Exceptions.ValidationException;

namespace DigitalAllianceTogo.Application.Fichiers.Commands
{
    /// <summary>
    /// Envoi d'un fichier (photo de livraison, signature, capture de paiement...).
    /// Renvoie le lien interne à mettre dans PreuveUrl / PhotoUrl / SignatureUrl.
    /// Le fichier est renommé (identifiant aléatoire) : le nom d'origine n'est jamais réutilisé.
    /// </summary>
    public record EnvoyerFichierCommand : IRequest<FichierEnvoyeDto>
    {
        public string Categorie { get; init; } = string.Empty;
        public long Taille { get; init; }
        public Stream Contenu { get; init; } = Stream.Null;
    }

    public record FichierEnvoyeDto(string Url, string ContentType, long Taille);

    public class EnvoyerFichierCommandValidator : AbstractValidator<EnvoyerFichierCommand>
    {
        public EnvoyerFichierCommandValidator()
        {
            RuleFor(x => x.Categorie)
                .Must(c => ReglesFichiers.Categories.Contains(c))
                .WithMessage($"Catégorie inconnue (attendu : {string.Join(", ", ReglesFichiers.Categories)}).");
            RuleFor(x => x.Taille)
                .GreaterThan(0).WithMessage("Le fichier est vide.")
                .LessThanOrEqualTo(ReglesFichiers.TailleMaximale).WithMessage("Le fichier dépasse 5 Mo : réduisez la photo.");
        }
    }

    public class EnvoyerFichierCommandHandler : IRequestHandler<EnvoyerFichierCommand, FichierEnvoyeDto>
    {
        private readonly IStockageFichiers _stockage;
        private readonly ICurrentUserService _user;

        public EnvoyerFichierCommandHandler(IStockageFichiers stockage, ICurrentUserService user)
        {
            _stockage = stockage;
            _user = user;
        }

        public async Task<FichierEnvoyeDto> Handle(EnvoyerFichierCommand request, CancellationToken cancellationToken)
        {
            // Les photos du catalogue sont publiques : seuls Admin et Catalogue les publient
            var catalogue = request.Categorie == ReglesFichiers.CategorieProduits;
            if (catalogue && !_user.EstDansRole(Roles.Admin) && !_user.EstDansRole(Roles.Catalogue))
                throw new ForbiddenAccessException();

            // Au plus 5 Mo (validé) : on peut lire en mémoire pour inspecter l'en-tête
            using var memoire = new MemoryStream();
            await request.Contenu.CopyToAsync(memoire, cancellationToken);
            if (memoire.Length > ReglesFichiers.TailleMaximale)
                throw Invalide("Le fichier dépasse 5 Mo : réduisez la photo.");

            var type = ReglesFichiers.DetecterType(memoire.GetBuffer().AsSpan(0, (int)Math.Min(16, memoire.Length)))
                ?? throw Invalide("Format non accepté : envoyez une photo (JPEG, PNG, WebP) ou un PDF.");
            if (catalogue && type == ReglesFichiers.Pdf)
                throw Invalide("Une photo de produit doit être au format JPEG, PNG ou WebP.");

            var nom = $"{Guid.NewGuid():N}.{type.Extension}";
            memoire.Position = 0;
            await _stockage.EnregistrerAsync(request.Categorie, nom, memoire, cancellationToken);

            return new FichierEnvoyeDto($"{ReglesFichiers.PrefixeLien}{request.Categorie}/{nom}", type.ContentType, memoire.Length);
        }

        private static ValidationException Invalide(string message) =>
            new(new[] { new FluentValidation.Results.ValidationFailure("Fichier", message) });
    }
}
