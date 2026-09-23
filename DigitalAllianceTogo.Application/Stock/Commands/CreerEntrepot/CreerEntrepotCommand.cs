using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Domain.Models.Stock;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Stock.Commands.CreerEntrepot
{
    public record CreerEntrepotCommand : IRequest<Guid>
    {
        public string Nom { get; init; } = string.Empty;
        public string Adresse { get; init; } = string.Empty;
    }

    public class CreerEntrepotCommandValidator : AbstractValidator<CreerEntrepotCommand>
    {
        public CreerEntrepotCommandValidator()
        {
            RuleFor(x => x.Nom).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Adresse).NotEmpty().MaximumLength(500);
        }
    }

    public class CreerEntrepotCommandHandler : IRequestHandler<CreerEntrepotCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAuditService _audit;

        public CreerEntrepotCommandHandler(IApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<Guid> Handle(CreerEntrepotCommand request, CancellationToken cancellationToken)
        {
            var nom = request.Nom.Trim();
            if (await _context.Entrepots.AnyAsync(e => e.Nom.ToLower() == nom.ToLower(), cancellationToken))
                throw new ConflictException($"L'entrepôt \"{nom}\" existe déjà.");

            var entrepot = new Entrepot { Id = Guid.NewGuid(), Nom = nom, Adresse = request.Adresse.Trim(), Actif = true };
            _context.Entrepots.Add(entrepot);
            _audit.Enregistrer("CreationEntrepot", "Entrepot", entrepot.Id, apres: new { entrepot.Nom, entrepot.Adresse });

            await _context.SaveChangesAsync(cancellationToken);
            return entrepot.Id;
        }
    }
}
