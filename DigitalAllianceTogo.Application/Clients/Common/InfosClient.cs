using DigitalAllianceTogo.Domain.Enum;
using FluentValidation;

namespace DigitalAllianceTogo.Application.Clients.Common
{
    /// <summary>Champs d'identité communs à la création, l'inscription et la modification d'un client.</summary>
    public interface IInfosClient
    {
        TypeClient Type { get; }
        string Nom { get; }
        string? Prenom { get; }
        string? RaisonSociale { get; }
        string Telephone { get; }
    }

    public class InfosClientValidator : AbstractValidator<IInfosClient>
    {
        public InfosClientValidator()
        {
            RuleFor(x => x.Type).IsInEnum();
            RuleFor(x => x.Nom).NotEmpty().WithMessage("Le nom est obligatoire.").MaximumLength(100);
            RuleFor(x => x.Prenom).MaximumLength(100);

            RuleFor(x => x.RaisonSociale)
                .NotEmpty().When(x => x.Type == TypeClient.Entreprise)
                .WithMessage("La raison sociale est obligatoire pour une entreprise.")
                .MaximumLength(200);

            // Format souple : +228 90 00 00 00, 90000000, 0022890000000...
            RuleFor(x => x.Telephone)
                .NotEmpty().WithMessage("Le téléphone est obligatoire.")
                .MaximumLength(30)
                .Matches(@"^\+?[0-9 ]{8,20}$").WithMessage("Le numéro de téléphone n'est pas valide.");
        }
    }
}
