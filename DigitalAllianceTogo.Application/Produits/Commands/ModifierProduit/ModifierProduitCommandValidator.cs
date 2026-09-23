using FluentValidation;

namespace DigitalAllianceTogo.Application.Produits.Commands.ModifierProduit
{
    public class ModifierProduitCommandValidator : AbstractValidator<ModifierProduitCommand>
    {
        public ModifierProduitCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Nom).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(4000);
            RuleFor(x => x.Prix).GreaterThan(0).WithMessage("Le prix doit être strictement positif.");
            RuleFor(x => x.CategorieId).NotEmpty();
            RuleFor(x => x.MarqueId).NotEmpty();
        }
    }
}
