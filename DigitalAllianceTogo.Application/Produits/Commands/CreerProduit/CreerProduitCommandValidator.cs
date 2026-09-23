using FluentValidation;

namespace DigitalAllianceTogo.Application.Produits.Commands.CreerProduit
{
    public class CreerProduitCommandValidator : AbstractValidator<CreerProduitCommand>
    {
        public CreerProduitCommandValidator()
        {
            RuleFor(x => x.Reference).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Nom).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(4000);
            RuleFor(x => x.Prix).GreaterThan(0).WithMessage("Le prix doit être strictement positif.");
            RuleFor(x => x.CategorieId).NotEmpty();
            RuleFor(x => x.MarqueId).NotEmpty();
        }
    }
}
