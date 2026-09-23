using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Models.Catalogue;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Catalogue
{
    /// <summary>Choisit l'image affichée en vignette : une seule principale par produit.</summary>
    public record DefinirImagePrincipaleCommand(Guid ImageId) : IRequest;

    public enum TypeReferentiel
    {
        Categorie,
        Marque
    }

    /// <summary>
    /// Modifie une catégorie ou une marque. La retirer (Actif = false) masque ses produits
    /// de la boutique sans toucher à l'historique (devis, commandes).
    /// </summary>
    public record ModifierReferentielCommand(TypeReferentiel Type, Guid Id, string Nom, string Description, bool Actif) : IRequest;

    public class ModifierReferentielCommandValidator : AbstractValidator<ModifierReferentielCommand>
    {
        public ModifierReferentielCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Nom).NotEmpty().WithMessage("Le nom est obligatoire.").MaximumLength(150);
            RuleFor(x => x.Description).MaximumLength(1000);
        }
    }

    public class CatalogueHandler :
        IRequestHandler<DefinirImagePrincipaleCommand>,
        IRequestHandler<ModifierReferentielCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAuditService _audit;

        public CatalogueHandler(IApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task Handle(DefinirImagePrincipaleCommand request, CancellationToken cancellationToken)
        {
            var image = await _context.ImagesProduit.FirstOrDefaultAsync(i => i.Id == request.ImageId, cancellationToken)
                ?? throw new NotFoundException(nameof(ImageProduit), request.ImageId);

            var images = await _context.ImagesProduit.Where(i => i.ProduitId == image.ProduitId).ToListAsync(cancellationToken);
            foreach (var i in images)
                i.EstPrincipale = i.Id == image.Id;

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task Handle(ModifierReferentielCommand request, CancellationToken cancellationToken)
        {
            var nom = request.Nom.Trim();
            var description = (request.Description ?? string.Empty).Trim();

            if (request.Type == TypeReferentiel.Categorie)
            {
                var categorie = await _context.Categories.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
                    ?? throw new NotFoundException(nameof(Categorie), request.Id);
                if (await _context.Categories.AnyAsync(c => c.Id != request.Id && c.Nom.ToLower() == nom.ToLower(), cancellationToken))
                    throw new ConflictException($"La catégorie \"{nom}\" existe déjà.");

                var avant = new { categorie.Nom, categorie.Actif };
                (categorie.Nom, categorie.Description, categorie.Actif) = (nom, description, request.Actif);
                _audit.Enregistrer("ModificationCategorie", "Categorie", categorie.Id, avant, new { categorie.Nom, categorie.Actif });
            }
            else
            {
                var marque = await _context.Marques.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken)
                    ?? throw new NotFoundException(nameof(Marque), request.Id);
                if (await _context.Marques.AnyAsync(m => m.Id != request.Id && m.Nom.ToLower() == nom.ToLower(), cancellationToken))
                    throw new ConflictException($"La marque \"{nom}\" existe déjà.");

                var avant = new { marque.Nom, marque.Actif };
                (marque.Nom, marque.Description, marque.Actif) = (nom, description, request.Actif);
                _audit.Enregistrer("ModificationMarque", "Marque", marque.Id, avant, new { marque.Nom, marque.Actif });
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
