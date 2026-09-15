using FluentValidation;

namespace DigitalAllianceTogo.Application.Utilisateurs.Queries.GetUtilisateurs
{
    public class GetUtilisateursQueryValidator : AbstractValidator<GetUtilisateursQuery>
    {
        public GetUtilisateursQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100)
                .WithMessage("La taille de page doit être comprise entre 1 et 100.");
        }
    }
}
