using FluentValidation;

namespace DigitalAllianceTogo.Application.Clients.Commands.Adresses
{
    public class AdresseInputValidator : AbstractValidator<AdresseInput>
    {
        public AdresseInputValidator()
        {
            RuleFor(x => x.Libelle).NotEmpty().WithMessage("Donnez un nom à l'adresse (ex : Maison, Bureau).").MaximumLength(100);
            RuleFor(x => x.Ligne1).NotEmpty().WithMessage("L'adresse est obligatoire.").MaximumLength(200);
            RuleFor(x => x.Ligne2).MaximumLength(200);
            RuleFor(x => x.Ville).NotEmpty().WithMessage("La ville est obligatoire.").MaximumLength(100);
            RuleFor(x => x.Pays).NotEmpty().MaximumLength(100);
            RuleFor(x => x.CodePostal).MaximumLength(20);
        }
    }

    public class AjouterAdresseCommandValidator : AbstractValidator<AjouterAdresseCommand>
    {
        public AjouterAdresseCommandValidator()
        {
            RuleFor(x => x.ClientId).NotEmpty();
            Include(new AdresseInputValidator());
        }
    }

    public class ModifierAdresseCommandValidator : AbstractValidator<ModifierAdresseCommand>
    {
        public ModifierAdresseCommandValidator()
        {
            RuleFor(x => x.ClientId).NotEmpty();
            RuleFor(x => x.AdresseId).NotEmpty();
            Include(new AdresseInputValidator());
        }
    }
}
