using FluentValidation;

namespace DigitalAllianceTogo.Application.Produits.Queries.GetProduits
{
    public class GetProduitsQueryValidator : AbstractValidator<GetProduitsQuery>
    {
        public GetProduitsQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}
