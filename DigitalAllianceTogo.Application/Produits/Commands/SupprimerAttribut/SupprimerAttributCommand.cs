using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Models.Catalogue;
using MediatR;

namespace DigitalAllianceTogo.Application.Produits.Commands.SupprimerAttribut
{
    public record SupprimerAttributCommand(Guid AttributId) : IRequest;
    public class SupprimerAttributCommandHandler : IRequestHandler<SupprimerAttributCommand>
    {
        private readonly IApplicationDbContext _context;

        public SupprimerAttributCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(SupprimerAttributCommand request, CancellationToken cancellationToken)
        {
            var attribut = await _context.AttributsProduit.FindAsync(new object[] { request.AttributId }, cancellationToken)
                ?? throw new NotFoundException(nameof(AttributProduit), request.AttributId);

            _context.AttributsProduit.Remove(attribut);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
