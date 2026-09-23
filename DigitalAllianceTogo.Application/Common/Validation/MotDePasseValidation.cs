using FluentValidation;

namespace DigitalAllianceTogo.Application.Common.Validation
{
    /// <summary>Règles de mot de passe partagées (création d'utilisateur, inscription client...).</summary>
    public static class MotDePasseValidation
    {
        public static IRuleBuilderOptions<T, string> MotDePasseRobuste<T>(this IRuleBuilder<T, string> ruleBuilder) =>
            ruleBuilder
                .NotEmpty().WithMessage("Le mot de passe est obligatoire.")
                .MinimumLength(8).WithMessage("Le mot de passe doit contenir au moins 8 caractères.")
                .Matches("[A-Z]").WithMessage("Le mot de passe doit contenir au moins une majuscule.")
                .Matches("[0-9]").WithMessage("Le mot de passe doit contenir au moins un chiffre.");
    }
}
