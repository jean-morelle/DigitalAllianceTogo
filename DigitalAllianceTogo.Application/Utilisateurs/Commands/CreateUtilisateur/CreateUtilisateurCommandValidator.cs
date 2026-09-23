using DigitalAllianceTogo.Application.Common.Validation;
using FluentValidation;

namespace DigitalAllianceTogo.Application.Utilisateurs.Commands.CreateUtilisateur
{
    public class CreateUtilisateurCommandValidator : AbstractValidator<CreateUtilisateurCommand>
    {
        public CreateUtilisateurCommandValidator()
        {
            RuleFor(x => x.Nom)
                .NotEmpty().WithMessage("Le nom est obligatoire.")
                .MaximumLength(100);

            RuleFor(x => x.Prenom)
                .NotEmpty().WithMessage("Le prénom est obligatoire.")
                .MaximumLength(100);

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("L'email est obligatoire.")
                .EmailAddress().WithMessage("L'email n'est pas valide.")
                .MaximumLength(255);

            RuleFor(x => x.Telephone)
                .MaximumLength(30);

            RuleFor(x => x.MotDePasse).MotDePasseRobuste();
        }
    }
}

