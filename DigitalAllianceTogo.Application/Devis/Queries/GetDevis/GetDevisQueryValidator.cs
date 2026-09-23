using FluentValidation;

namespace DigitalAllianceTogo.Application.Devis.Queries.GetDevis
{
    public class GetDevisQueryValidator : AbstractValidator<GetDevisQuery>
    {
        public GetDevisQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}
