using DigitalAllianceTogo.Application.Devis.Common;
using FluentValidation;

namespace DigitalAllianceTogo.Application.Devis.Commands.CreerDevis
{
    public class CreerDevisCommandValidator : AbstractValidator<CreerDevisCommand>
    {
        public CreerDevisCommandValidator()
        {
            RuleFor(x => x.ClientId).NotEmpty();
            RuleFor(x => x.RemiseGlobale).GreaterThanOrEqualTo(0).WithMessage("La remise ne peut pas être négative.");

            RuleFor(x => x.Lignes)
                .NotEmpty().WithMessage("Un devis doit contenir au moins une ligne.")
                .Must(l => l.Select(x => x.ProduitId).Distinct().Count() == l.Count)
                .WithMessage("Un même produit ne peut apparaître qu'une fois : augmentez plutôt sa quantité.");

            RuleForEach(x => x.Lignes).SetValidator(new LigneDevisInputValidator());
        }
    }
}
