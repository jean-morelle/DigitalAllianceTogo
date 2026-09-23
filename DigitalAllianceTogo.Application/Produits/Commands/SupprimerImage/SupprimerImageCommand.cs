using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Models.Catalogue;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Produits.Commands.SupprimerAttribut
{
    public record SupprimerImageCommand(Guid ImageId) : IRequest;
    public class SupprimerImageCommandHandler : IRequestHandler<SupprimerImageCommand>
    {
        private readonly IApplicationDbContext _context;

        public SupprimerImageCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(SupprimerImageCommand request, CancellationToken cancellationToken)
        {
            var image = await _context.ImagesProduit.FindAsync(new object[] { request.ImageId }, cancellationToken)
                ?? throw new NotFoundException(nameof(ImageProduit), request.ImageId);

            _context.ImagesProduit.Remove(image);

            // L'image principale supprimée : la suivante prend le relais
            if (image.EstPrincipale)
            {
                var suivante = await _context.ImagesProduit
                    .Where(i => i.ProduitId == image.ProduitId && i.Id != image.Id)
                    .OrderBy(i => i.Ordre)
                    .FirstOrDefaultAsync(cancellationToken);
                if (suivante is not null)
                    suivante.EstPrincipale = true;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
