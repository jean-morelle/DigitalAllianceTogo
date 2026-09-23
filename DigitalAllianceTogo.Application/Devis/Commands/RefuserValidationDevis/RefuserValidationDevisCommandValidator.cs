using FluentValidation;

namespace DigitalAllianceTogo.Application.Devis.Commands.RefuserValidationDevis
{
    public class RefuserValidationDevisCommandValidator : AbstractValidator<RefuserValidationDevisCommand>
    {
        public RefuserValidationDevisCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Motif).NotEmpty().WithMessage("Le motif du refus est obligatoire.").MaximumLength(1000);
        }
    }
}
