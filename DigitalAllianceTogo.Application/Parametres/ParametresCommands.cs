using DigitalAllianceTogo.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Parametres
{
    public record ParametresDto(
        decimal SeuilRemiseCommercialPourcent,
        decimal SeuilAugmentationModificationPourcent,
        int DelaiExpirationPaiementHeures,
        int DureeValiditeDevisJours,
        string? NumeroTMoney,
        string? NumeroFlooz,
        string? NomBeneficiairePaiement,
        DateTime DateModification);

    /// <summary>Ce que le client doit savoir pour payer : numéros Mobile Money et bénéficiaire.</summary>
    public record InfosPaiementDto(string? NumeroTMoney, string? NumeroFlooz, string? NomBeneficiaire, int DelaiPaiementHeures);

    /// <summary>Paramètres complets (Administrateur).</summary>
    public record GetParametresQuery : IRequest<ParametresDto>;

    /// <summary>Informations de paiement, publiques.</summary>
    public record GetInfosPaiementQuery : IRequest<InfosPaiementDto>;

    /// <summary>Modification des paramètres par l'Administrateur ; tracée dans le journal d'audit.</summary>
    public record ModifierParametresCommand : IRequest
    {
        public decimal SeuilRemiseCommercialPourcent { get; init; }
        public decimal SeuilAugmentationModificationPourcent { get; init; }
        public int DelaiExpirationPaiementHeures { get; init; }
        public int DureeValiditeDevisJours { get; init; }
        public string? NumeroTMoney { get; init; }
        public string? NumeroFlooz { get; init; }
        public string? NomBeneficiairePaiement { get; init; }
    }

    public class ModifierParametresCommandValidator : AbstractValidator<ModifierParametresCommand>
    {
        // Chiffres, espaces et « + » : format international (+228 90 00 00 00) ou local
        private const string FormatNumero = @"^\+?[0-9 ]{8,20}$";

        public ModifierParametresCommandValidator()
        {
            RuleFor(x => x.SeuilRemiseCommercialPourcent).InclusiveBetween(0m, 100m);
            RuleFor(x => x.SeuilAugmentationModificationPourcent).InclusiveBetween(0m, 100m);
            RuleFor(x => x.DelaiExpirationPaiementHeures).InclusiveBetween(1, 24 * 30);
            RuleFor(x => x.DureeValiditeDevisJours).InclusiveBetween(1, 365);
            RuleFor(x => x.NumeroTMoney).Matches(FormatNumero).When(x => !string.IsNullOrWhiteSpace(x.NumeroTMoney))
                .WithMessage("Numéro T-Money invalide.");
            RuleFor(x => x.NumeroFlooz).Matches(FormatNumero).When(x => !string.IsNullOrWhiteSpace(x.NumeroFlooz))
                .WithMessage("Numéro Flooz invalide.");
            RuleFor(x => x.NomBeneficiairePaiement).MaximumLength(100);
        }
    }

    public class ParametresHandler :
        IRequestHandler<GetParametresQuery, ParametresDto>,
        IRequestHandler<GetInfosPaiementQuery, InfosPaiementDto>,
        IRequestHandler<ModifierParametresCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAuditService _audit;

        public ParametresHandler(IApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<ParametresDto> Handle(GetParametresQuery request, CancellationToken cancellationToken)
        {
            var p = await _context.ParametresEntreprise.AsNoTracking().FirstAsync(cancellationToken);
            return VersDto(p);
        }

        public async Task<InfosPaiementDto> Handle(GetInfosPaiementQuery request, CancellationToken cancellationToken)
        {
            var p = await _context.ParametresEntreprise.AsNoTracking().FirstAsync(cancellationToken);
            return new InfosPaiementDto(p.NumeroTMoney, p.NumeroFlooz, p.NomBeneficiairePaiement, p.DelaiExpirationPaiementHeures);
        }

        public async Task Handle(ModifierParametresCommand request, CancellationToken cancellationToken)
        {
            var p = await _context.ParametresEntreprise.FirstAsync(cancellationToken);
            var avant = VersDto(p);

            p.SeuilRemiseCommercialPourcent = request.SeuilRemiseCommercialPourcent;
            p.SeuilAugmentationModificationPourcent = request.SeuilAugmentationModificationPourcent;
            p.DelaiExpirationPaiementHeures = request.DelaiExpirationPaiementHeures;
            p.DureeValiditeDevisJours = request.DureeValiditeDevisJours;
            p.NumeroTMoney = Nettoyer(request.NumeroTMoney);
            p.NumeroFlooz = Nettoyer(request.NumeroFlooz);
            p.NomBeneficiairePaiement = Nettoyer(request.NomBeneficiairePaiement);
            p.DateModification = DateTime.UtcNow;

            _audit.Enregistrer("ModificationParametres", "ParametresEntreprise", p.Id, avant, VersDto(p));
            await _context.SaveChangesAsync(cancellationToken);
        }

        private static ParametresDto VersDto(Domain.Models.Parametres.ParametresEntreprise p) =>
            new(p.SeuilRemiseCommercialPourcent, p.SeuilAugmentationModificationPourcent,
                p.DelaiExpirationPaiementHeures, p.DureeValiditeDevisJours,
                p.NumeroTMoney, p.NumeroFlooz, p.NomBeneficiairePaiement, p.DateModification);

        private static string? Nettoyer(string? valeur) => string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();
    }
}
