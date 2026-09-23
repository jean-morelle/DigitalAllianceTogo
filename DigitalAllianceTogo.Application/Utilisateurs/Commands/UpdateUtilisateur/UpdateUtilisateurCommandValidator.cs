using FluentValidation;

namespace DigitalAllianceTogo.Application.Utilisateurs.Commands.UpdateUtilisateur
{
    public class UpdateUtilisateurCommandValidator : AbstractValidator<UpdateUtilisateurCommand>
    {
        public UpdateUtilisateurCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();

            RuleFor(x => x.Nom)
                .NotEmpty().WithMessage("Le nom est obligatoire.")
                .MaximumLength(100);

            RuleFor(x => x.Prenom)
                .NotEmpty().WithMessage("Le prénom est obligatoire.")
                .MaximumLength(100);

            RuleFor(x => x.Telephone)
                .MaximumLength(30);
        }
    }
}
