using FluentValidation;

namespace DigitalAllianceTogo.Application.Auth.Commands.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("L'email est obligatoire.")
                .EmailAddress().WithMessage("L'email n'est pas valide.");

            RuleFor(x => x.MotDePasse)
                .NotEmpty().WithMessage("Le mot de passe est obligatoire.");
        }
    }
}
