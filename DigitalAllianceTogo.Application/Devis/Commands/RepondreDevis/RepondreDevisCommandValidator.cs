using FluentValidation;

namespace DigitalAllianceTogo.Application.Devis.Commands.RepondreDevis
{
    public class RepondreDevisCommandValidator : AbstractValidator<RepondreDevisCommand>
    {
        public RepondreDevisCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Reponse).IsInEnum();
            RuleFor(x => x.Commentaire).MaximumLength(1000);

            // Le commercial doit savoir QUOI modifier
            RuleFor(x => x.Commentaire)
                .NotEmpty()
                .When(x => x.Reponse == ReponseClientDevis.DemanderModification)
                .WithMessage("Précisez la modification souhaitée.");
        }
    }
}
