using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Models.Catalogue;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Produits.Commands.AjouterAttribut
{
    public record AjouterAttributCommand : IRequest<Guid>
    {
        public Guid ProduitId { get; init; }
        public string Cle { get; init; } = string.Empty;
        public string Valeur { get; init; } = string.Empty;
        public int Ordre { get; init; }
    }
    public class AjouterAttributCommandHandler : IRequestHandler<AjouterAttributCommand, Guid>
    {
        private readonly IApplicationDbContext _context;

        public AjouterAttributCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(AjouterAttributCommand request, CancellationToken cancellationToken)
        {
            var produitExiste = await _context.Produits.AnyAsync(p => p.Id == request.ProduitId, cancellationToken);
            if (!produitExiste)
                throw new NotFoundException(nameof(Produit), request.ProduitId);

            var attribut = new AttributProduit
            {
                Id = Guid.NewGuid(),
                ProduitId = request.ProduitId,
                Cle = request.Cle,
                Valeur = request.Valeur,
                Ordre = request.Ordre
            };

            _context.AttributsProduit.Add(attribut);
            await _context.SaveChangesAsync(cancellationToken);

            return attribut.Id;
        }
    }
}
