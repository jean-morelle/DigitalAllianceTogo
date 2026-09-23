using DigitalAllianceTogo.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Marques.Queries.GetMarques
{

    public record MarqueDto(Guid Id, string Nom, string Description, bool Actif);
    public record GetMarquesQuery : IRequest<List<MarqueDto>>;
    public class GetMarquesQueryHandler : IRequestHandler<GetMarquesQuery, List<MarqueDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetMarquesQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<MarqueDto>> Handle(GetMarquesQuery request, CancellationToken cancellationToken)
        {
            return await _context.Marques
                .AsNoTracking()
                .OrderBy(m => m.Nom)
                .Select(m => new MarqueDto(m.Id, m.Nom, m.Description, m.Actif))
                .ToListAsync(cancellationToken);
        }
    }
}
