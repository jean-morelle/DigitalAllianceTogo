using DigitalAllianceTogo.Application.Clients.Common;
using DigitalAllianceTogo.Application.Common.Validation;
using FluentValidation;

namespace DigitalAllianceTogo.Application.Clients.Commands.InscrireClient
{
    public class InscrireClientCommandValidator : AbstractValidator<InscrireClientCommand>
    {
        public InscrireClientCommandValidator()
        {
            Include(new InfosClientValidator());
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("L'email est obligatoire.")
                .EmailAddress().WithMessage("L'email n'est pas valide.")
                .MaximumLength(255);
            RuleFor(x => x.MotDePasse).MotDePasseRobuste();
            RuleFor(x => x.Source).IsInEnum();
        }
    }
}
