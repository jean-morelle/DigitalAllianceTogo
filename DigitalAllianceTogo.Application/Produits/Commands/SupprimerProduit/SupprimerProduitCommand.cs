using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Models.Catalogue;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

            // Un produit déjà stocké, vendu ou proposé en devis reste dans l'historique :
            // on le désactive (Actif = false) au lieu de le supprimer.
            var utilise = await _context.StocksProduit.AnyAsync(s => s.ProduitId == produit.Id, cancellationToken)
                || await _context.LignesCommande.AnyAsync(l => l.ProduitId == produit.Id, cancellationToken)
                || await _context.LignesDevis.AnyAsync(l => l.ProduitId == produit.Id, cancellationToken);
            if (utilise)
                throw new ConflictException("Ce produit a déjà du stock, des devis ou des commandes : désactivez-le plutôt que de le supprimer.");

            // Retiré des paniers en cours
            _context.LignesPanier.RemoveRange(_context.LignesPanier.Where(l => l.ProduitId == produit.Id));
            _context.Produits.Remove(produit);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
