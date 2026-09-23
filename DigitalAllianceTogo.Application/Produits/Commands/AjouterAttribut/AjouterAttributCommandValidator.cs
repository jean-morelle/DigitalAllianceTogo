using FluentValidation;

namespace DigitalAllianceTogo.Application.Produits.Commands.AjouterAttribut
{
    public class AjouterAttributCommandValidator : AbstractValidator<AjouterAttributCommand>
    {
        public AjouterAttributCommandValidator()
        {
            RuleFor(x => x.ProduitId).NotEmpty();
            RuleFor(x => x.Cle).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Valeur).NotEmpty().MaximumLength(500);
        }
    }
}
