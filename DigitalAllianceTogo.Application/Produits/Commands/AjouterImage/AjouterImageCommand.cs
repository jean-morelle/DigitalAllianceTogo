using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Models.Catalogue;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Produits.Commands.AjouterImage
{
    public record AjouterImageCommand : IRequest<Guid>
    {
        public Guid ProduitId { get; init; }
        public string Url { get; init; } = string.Empty;
        public int Ordre { get; init; }
        public bool EstPrincipale { get; init; }
    }
    public class AjouterImageCommandHandler : IRequestHandler<AjouterImageCommand, Guid>
    {
        private readonly IApplicationDbContext _context;

        public AjouterImageCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(AjouterImageCommand request, CancellationToken cancellationToken)
        {
            var produitExiste = await _context.Produits.AnyAsync(p => p.Id == request.ProduitId, cancellationToken);
            if (!produitExiste)
                throw new NotFoundException(nameof(Produit), request.ProduitId);

            var imagesExistantes = await _context.ImagesProduit
                .Where(i => i.ProduitId == request.ProduitId)
                .ToListAsync(cancellationToken);

            // La première image devient principale d'office : la boutique a toujours une vignette
            var principale = request.EstPrincipale || !imagesExistantes.Any(i => i.EstPrincipale);

            // Une seule image principale par produit : si la nouvelle l'est,
            // on désactive l'ancienne pour ne jamais en avoir deux en même temps.
            if (principale)
                foreach (var image in imagesExistantes)
                    image.EstPrincipale = false;

            var nouvelleImage = new ImageProduit
            {
                Id = Guid.NewGuid(),
                ProduitId = request.ProduitId,
                Url = request.Url,
                Ordre = request.Ordre > 0 ? request.Ordre : imagesExistantes.Select(i => i.Ordre).DefaultIfEmpty(0).Max() + 1,
                EstPrincipale = principale
            };

            _context.ImagesProduit.Add(nouvelleImage);
            await _context.SaveChangesAsync(cancellationToken);

            return nouvelleImage.Id;
        }

    }
}