using FluentValidation;

namespace DigitalAllianceTogo.Application.Paiements.Commands.RejeterPaiement
{
    public class RejeterPaiementCommandValidator : AbstractValidator<RejeterPaiementCommand>
    {
        public RejeterPaiementCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            // Le client doit comprendre pourquoi sa preuve est refusée
            RuleFor(x => x.Motif)
                .NotEmpty().WithMessage("Indiquez le motif du rejet.")
                .MaximumLength(500);
        }
    }
}
