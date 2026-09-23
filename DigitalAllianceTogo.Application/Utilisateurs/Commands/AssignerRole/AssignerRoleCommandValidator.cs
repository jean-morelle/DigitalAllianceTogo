using FluentValidation;

namespace DigitalAllianceTogo.Application.Utilisateurs.Commands.AssignerRole
{
    internal class AssignerRoleCommandValidator : AbstractValidator<AssignerRoleCommand>
    {
        public AssignerRoleCommandValidator()
        {
            RuleFor(x => x.UtilisateurId).NotEmpty();
            RuleFor(x => x.RoleId).NotEmpty();
        }
    }
}
