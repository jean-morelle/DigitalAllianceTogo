using DigitalAllianceTogo.Application.Clients.Common;
using FluentValidation;

namespace DigitalAllianceTogo.Application.Clients.Commands.ModifierClient
{
    public class ModifierClientCommandValidator : AbstractValidator<ModifierClientCommand>
    {
        public ModifierClientCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            Include(new InfosClientValidator());
            RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
                .WithMessage("L'email n'est pas valide.").MaximumLength(255);
            RuleFor(x => x.Source).IsInEnum().When(x => x.Source.HasValue);
        }
    }
}
