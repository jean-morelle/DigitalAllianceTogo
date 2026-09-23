using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Models.Catalogue;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Categories.Commands.CreerCategorie
{
    public record CreerCategorieCommand : IRequest<Guid>
    {
        public string Nom { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
    }

    public class CreerCategorieCommandValidator : AbstractValidator<CreerCategorieCommand>
    {
        public CreerCategorieCommandValidator()
        {
            RuleFor(x => x.Nom).NotEmpty().WithMessage("Le nom est obligatoire.").MaximumLength(150);
            RuleFor(x => x.Description).MaximumLength(1000);
        }
    }

    public class CreerCategorieCommandHandler : IRequestHandler<CreerCategorieCommand, Guid>
    {
        private readonly IApplicationDbContext _context;

        public CreerCategorieCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(CreerCategorieCommand request, CancellationToken cancellationToken)
        {
            var nom = request.Nom.Trim();
            if (await _context.Categories.AnyAsync(c => c.Nom.ToLower() == nom.ToLower(), cancellationToken))
                throw new ConflictException($"La catégorie \"{nom}\" existe déjà.");

            var categorie = new Categorie { Id = Guid.NewGuid(), Nom = nom, Description = request.Description.Trim(), Actif = true };
            _context.Categories.Add(categorie);
            await _context.SaveChangesAsync(cancellationToken);
            return categorie.Id;
        }
    }
}
