using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Models.Catalogue;
using MediatR;

namespace DigitalAllianceTogo.Application.Produits.Commands.SupprimerProduit
{
    public record SupprimerProduitCommand(Guid Id) : IRequest;

    public class SupprimerProduitCommandHandler : IRequestHandler<SupprimerProduitCommand>
    {
        private readonly IApplicationDbContext _context;

        public SupprimerProduitCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Handle(SupprimerProduitCommand request, CancellationToken cancellationToken)
        {
            var produit = await _context.Produits.FindAsync(new object[] { request.Id }, cancellationToken)
                ?? throw new NotFoundException(nameof(Produit), request.Id);

            // Les FK vers StockProduit/LigneCommande/LigneDevis/LignePanier sont en
            // Restrict (voir ProduitConfiguration) : la suppression échouera avec une
            // exception SQL si le produit a déjà été vendu ou stocké quelque part.
            // C'est voulu — dans ce cas, désactiver (Actif = false) via ModifierProduit
            // est la bonne opération, pas la suppression physique.
            _context.Produits.Remove(produit);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
