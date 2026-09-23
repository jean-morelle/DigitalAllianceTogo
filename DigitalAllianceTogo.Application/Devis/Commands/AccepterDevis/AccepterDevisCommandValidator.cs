using FluentValidation;

namespace DigitalAllianceTogo.Application.Devis.Commands.AccepterDevis
{
    public class AccepterDevisCommandValidator : AbstractValidator<AccepterDevisCommand>
    {
        public AccepterDevisCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.AdresseLivraisonId).NotEmpty().WithMessage("L'adresse de livraison est obligatoire.");
            RuleFor(x => x.TelephoneContact).MaximumLength(30);
        }
    }
}
