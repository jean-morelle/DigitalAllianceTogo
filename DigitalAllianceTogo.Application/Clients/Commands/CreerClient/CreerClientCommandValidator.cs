using DigitalAllianceTogo.Application.Clients.Common;
using FluentValidation;

namespace DigitalAllianceTogo.Application.Clients.Commands.CreerClient
{
    public class CreerClientCommandValidator : AbstractValidator<CreerClientCommand>
    {
        public CreerClientCommandValidator()
        {
            Include(new InfosClientValidator());
            RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
                .WithMessage("L'email n'est pas valide.").MaximumLength(255);
            RuleFor(x => x.Source).IsInEnum();
        }
    }
}
