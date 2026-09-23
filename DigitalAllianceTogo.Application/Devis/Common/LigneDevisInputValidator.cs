using FluentValidation;

namespace DigitalAllianceTogo.Application.Devis.Common
{
    public class LigneDevisInputValidator : AbstractValidator<LigneDevisInput>
    {
        public LigneDevisInputValidator()
        {
            RuleFor(x => x.ProduitId).NotEmpty();
            RuleFor(x => x.Quantite).GreaterThan(0).WithMessage("La quantité doit être strictement positive.");
            RuleFor(x => x.Remise).GreaterThanOrEqualTo(0).WithMessage("La remise ne peut pas être négative.");
        }
    }
}
