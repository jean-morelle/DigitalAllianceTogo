using DigitalAllianceTogo.Application.Fichiers;
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
                // Capture envoyée à l'API (/api/fichiers/...) ou lien web
                .Must(ReglesFichiers.EstLienPreuveValide)
                .When(x => !string.IsNullOrWhiteSpace(x.PreuveUrl))
                .WithMessage("La preuve doit être un fichier envoyé ou une adresse web valide.");
        }
    }
}
