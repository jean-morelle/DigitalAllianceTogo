using FluentValidation;

namespace DigitalAllianceTogo.Application.Utilisateurs.Commonds.CreateUtilisateur
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

            RuleFor(x => x.MotDePasse)
                .NotEmpty().WithMessage("Le mot de passe est obligatoire.")
                .MinimumLength(8).WithMessage("Le mot de passe doit contenir au moins 8 caractères.")
                .Matches("[A-Z]").WithMessage("Le mot de passe doit contenir au moins une majuscule.")
                .Matches("[0-9]").WithMessage("Le mot de passe doit contenir au moins un chiffre.");
        }
    }
}

