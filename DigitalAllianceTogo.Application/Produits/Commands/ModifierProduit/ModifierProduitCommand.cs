using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Models.Catalogue;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Produits.Commands.ModifierProduit
{
    public record ModifierProduitCommand : IRequest
    {
        public Guid Id { get; init; }
        public string Nom { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public decimal Prix { get; init; }
        public Guid CategorieId { get; init; }
        public Guid MarqueId { get; init; }
        public bool Actif { get; init; }
    }
    public class ModifierProduitCommandHandler : IRequestHandler<ModifierProduitCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAuditService _audit;

        public ModifierProduitCommandHandler(IApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task Handle(ModifierProduitCommand request, CancellationToken cancellationToken)
        {
            var produit = await _context.Produits.FindAsync(new object[] { request.Id }, cancellationToken)
                ?? throw new NotFoundException(nameof(Produit), request.Id);

            var categorieExiste = await _context.Categories.AnyAsync(c => c.Id == request.CategorieId, cancellationToken);
            if (!categorieExiste)
                throw new NotFoundException(nameof(Categorie), request.CategorieId);

            var marqueExiste = await _context.Marques.AnyAsync(m => m.Id == request.MarqueId, cancellationToken);
            if (!marqueExiste)
                throw new NotFoundException(nameof(Marque), request.MarqueId);

            // Note : Reference n'est volontairement PAS modifiable ici — c'est un
            // identifiant métier stable, le changer casserait la traçabilité des
            // lignes de commande/devis existantes qui affichent cette référence.
            var avant = new { produit.Nom, produit.Prix, produit.Actif, produit.CategorieId, produit.MarqueId };

            produit.Nom = request.Nom;
            produit.Description = request.Description;
            produit.Prix = request.Prix;
            produit.CategorieId = request.CategorieId;
            produit.MarqueId = request.MarqueId;
            produit.Actif = request.Actif;

            // Prix et mise en vente : tracés (un changement de prix se voit sur les ventes)
            _audit.Enregistrer("ModificationProduit", "Produit", produit.Id, avant,
                new { produit.Nom, produit.Prix, produit.Actif, produit.CategorieId, produit.MarqueId });

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
