using FluentValidation;

namespace DigitalAllianceTogo.Application.Clients.Queries.GetClients
{
    public class GetClientsQueryValidator : AbstractValidator<GetClientsQuery>
    {
        public GetClientsQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}
