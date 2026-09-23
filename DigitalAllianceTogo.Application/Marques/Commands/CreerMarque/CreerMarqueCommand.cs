using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Models.Catalogue;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Marques.Commands.CreerMarque
{
    public record CreerMarqueCommand : IRequest<Guid>
    {
        public string Nom { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
    }

    public class CreerMarqueCommandValidator : AbstractValidator<CreerMarqueCommand>
    {
        public CreerMarqueCommandValidator()
        {
            RuleFor(x => x.Nom).NotEmpty().WithMessage("Le nom est obligatoire.").MaximumLength(150);
            RuleFor(x => x.Description).MaximumLength(1000);
        }
    }

    public class CreerMarqueCommandHandler : IRequestHandler<CreerMarqueCommand, Guid>
    {
        private readonly IApplicationDbContext _context;

        public CreerMarqueCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(CreerMarqueCommand request, CancellationToken cancellationToken)
        {
            var nom = request.Nom.Trim();
            if (await _context.Marques.AnyAsync(m => m.Nom.ToLower() == nom.ToLower(), cancellationToken))
                throw new ConflictException($"La marque \"{nom}\" existe déjà.");

            var marque = new Marque { Id = Guid.NewGuid(), Nom = nom, Description = request.Description.Trim(), Actif = true };
            _context.Marques.Add(marque);
            await _context.SaveChangesAsync(cancellationToken);
            return marque.Id;
        }
    }
}
