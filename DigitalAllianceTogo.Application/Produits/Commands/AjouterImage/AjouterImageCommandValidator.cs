using DigitalAllianceTogo.Application.Fichiers;
using FluentValidation;

namespace DigitalAllianceTogo.Application.Produits.Commands.AjouterImage
{
    public class AjouterImageCommandValidator : AbstractValidator<AjouterImageCommand>
    {
        public AjouterImageCommandValidator()
        {
            RuleFor(x => x.ProduitId).NotEmpty();
            RuleFor(x => x.Url).NotEmpty().MaximumLength(1000).Must(ReglesFichiers.EstLienImageProduitValide)
                .WithMessage("L'URL de l'image n'est pas valide.");
        }
    }
}
