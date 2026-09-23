using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Models.Catalogue;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Produits.Commands.CreerProduit
{
    public class CreerProduitCommand : IRequest<Guid>
    {
        public string Reference { get; init; } = string.Empty;
        public string Nom { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public decimal Prix { get; init; }
        public Guid CategorieId { get; init; }
        public Guid MarqueId { get; init; }
    }
    public class CreerProduitCommandHandler : IRequestHandler<CreerProduitCommand, Guid>
    {
        private readonly IApplicationDbContext _context;

        public CreerProduitCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(CreerProduitCommand request, CancellationToken cancellationToken)
        {
            var referenceExiste = await _context.Produits
                .AnyAsync(p => p.Reference == request.Reference, cancellationToken);
            if (referenceExiste)
                throw new ConflictException($"Un produit avec la référence \"{request.Reference}\" existe déjà.");

            var categorieExiste = await _context.Categories
                .AnyAsync(c => c.Id == request.CategorieId, cancellationToken);
            if (!categorieExiste)
                throw new NotFoundException(nameof(Categorie), request.CategorieId);

            var marqueExiste = await _context.Marques
                .AnyAsync(m => m.Id == request.MarqueId, cancellationToken);
            if (!marqueExiste)
                throw new NotFoundException(nameof(Marque), request.MarqueId);

            var produit = new Produit
            {
                Id = Guid.NewGuid(),
                Reference = request.Reference,
                Nom = request.Nom,
                Description = request.Description,
                Prix = request.Prix,
                CategorieId = request.CategorieId,
                MarqueId = request.MarqueId,
                Actif = true,
                DateCreation = DateTime.UtcNow
            };

            _context.Produits.Add(produit);
            await _context.SaveChangesAsync(cancellationToken);

            return produit.Id;
        }

    }
}