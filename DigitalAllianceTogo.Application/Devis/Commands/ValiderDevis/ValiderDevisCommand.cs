using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Devis.Common;
using DigitalAllianceTogo.Domain.Enum;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Devis.Commands.ValiderDevis
{
    /// <summary>
    /// Validation interne du devis par l'entreprise (≠ acceptation par le client).
    ///
    /// - Remise ≤ seuil (10 % par défaut)  : le Commercial valide lui-même.
    /// - Remise &gt; seuil, appelée par un Commercial : le devis passe en ValidationInterne,
    ///   il attend l'Administrateur.
    /// - Appelée par l'Administrateur : validation quel que soit le taux.
    /// </summary>
    public record ValiderDevisCommand(Guid Id) : IRequest<ValiderDevisResult>;

    public record ValiderDevisResult(bool Valide, string Statut, string Message);

    public class ValiderDevisCommandHandler : IRequestHandler<ValiderDevisCommand, ValiderDevisResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditService _audit;

        public ValiderDevisCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
        {
            _context = context;
            _currentUser = currentUser;
            _audit = audit;
        }

        public async Task<ValiderDevisResult> Handle(ValiderDevisCommand request, CancellationToken cancellationToken)
        {
            var devis = await DevisHelper.ChargerAvecControleAccesAsync(_context, _currentUser, request.Id, cancellationToken);

            if (devis.Statut is not (StatutDevis.Brouillon or StatutDevis.ValidationInterne))
                throw new ConflictException($"Un devis au statut {devis.Statut} ne peut pas être validé.");

            if (devis.ValideParId is not null)
                throw new ConflictException("Ce devis est déjà validé.");

            var parametres = await _context.ParametresEntreprise.AsNoTracking().FirstAsync(cancellationToken);
            var estAdmin = _currentUser.EstDansRole(Roles.Admin);
            var avant = DevisHelper.Instantane(devis);

            if (!estAdmin && devis.TauxRemise > parametres.SeuilRemiseCommercialPourcent)
            {
                if (devis.Statut == StatutDevis.ValidationInterne)
                    throw new ConflictException("Ce devis attend déjà la validation de l'Administrateur.");

                devis.Statut = StatutDevis.ValidationInterne;
                devis.CommentaireInterne = null;

                _audit.Enregistrer("SoumissionValidationAdmin", "Devis", devis.Id, avant, DevisHelper.Instantane(devis));
                await _context.SaveChangesAsync(cancellationToken);

                return new ValiderDevisResult(false, devis.Statut.ToString(),
                    $"Remise de {devis.TauxRemise} % supérieure au seuil de {parametres.SeuilRemiseCommercialPourcent} % : " +
                    "le devis est soumis à la validation de l'Administrateur.");
            }

            devis.ValideParId = _currentUser.UtilisateurId;
            devis.DateValidation = DateTime.UtcNow;

            _audit.Enregistrer(estAdmin ? "ValidationDevisAdmin" : "ValidationDevisCommercial", "Devis", devis.Id,
                avant, DevisHelper.Instantane(devis));
            await _context.SaveChangesAsync(cancellationToken);

            return new ValiderDevisResult(true, devis.Statut.ToString(), "Devis validé : il peut être envoyé au client.");
        }
    }
}
