using FluentValidation;

namespace DigitalAllianceTogo.Application.Paiements.Commands.SoumettrePaiement
{
    public class SoumettrePaiementCommandValidator : AbstractValidator<SoumettrePaiementCommand>
    {
        public SoumettrePaiementCommandValidator()
        {
            RuleFor(x => x.CommandeId).NotEmpty();
            RuleFor(x => x.ReferenceExterne)
                .NotEmpty().WithMessage("La référence de la transaction est obligatoire.")
                .MaximumLength(100);
            RuleFor(x => x.PreuveUrl)
                .MaximumLength(500)
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
                .When(x => !string.IsNullOrWhiteSpace(x.PreuveUrl))
                .WithMessage("Le lien de la preuve doit être une adresse web valide.");
        }
    }
}
