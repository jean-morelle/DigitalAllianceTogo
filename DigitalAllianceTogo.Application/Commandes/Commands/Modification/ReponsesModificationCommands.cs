using DigitalAllianceTogo.Application.Commandes.Common;
using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Finance.Common;
using DigitalAllianceTogo.Application.Stock.Common;
using DigitalAllianceTogo.Domain.Enum;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Commandes.Commands.Modification
{
    /// <summary>L'Administrateur valide (→ envoyée au client) ou refuse une proposition hors seuil (§21).</summary>
    public record DeciderModificationAdminCommand : IRequest
    {
        public Guid CommandeId { get; init; }
        public bool Valider { get; init; }
        public string? Motif { get; init; }
    }

    /// <summary>Le Commercial retire sa proposition (le client a changé d'avis, erreur de saisie...).</summary>
    public record RetirerModificationCommand : IRequest
    {
        public Guid CommandeId { get; init; }
        public string Motif { get; init; } = string.Empty;
    }

    /// <summary>
    /// Le client accepte ou refuse la nouvelle version (§20). S'il accepte, elle devient active
    /// (l'ancienne reste consultable, avec ses paiements) et, selon le nouveau prix (§21) :
    /// - identique : la commande reprend avec le stock ajusté ;
    /// - supérieur : le complément doit être payé et confirmé avant de poursuivre ;
    /// - inférieur : le trop-perçu est rendu par remboursement ou avoir (validation Admin).
    /// Une commande en préparation repasse par la préparation.
    /// </summary>
    public record RepondreModificationCommand : IRequest<RepondreModificationResult>
    {
        public Guid CommandeId { get; init; }
        public bool Accepter { get; init; }

        /// <summary>Baisse de prix : remboursement (par défaut) ou avoir.</summary>
        public ModeRegularisation Regularisation { get; init; } = ModeRegularisation.Remboursement;
        public string? Motif { get; init; }
    }

    public record RepondreModificationResult(string StatutCommande, int VersionActive, decimal ResteAPayer, decimal ARendre, string Message);

    public class DeciderModificationAdminCommandValidator : AbstractValidator<DeciderModificationAdminCommand>
    {
        public DeciderModificationAdminCommandValidator()
        {
            RuleFor(x => x.CommandeId).NotEmpty();
            RuleFor(x => x.Motif).NotEmpty().When(x => !x.Valider).WithMessage("Indiquez pourquoi la modification est refusée.").MaximumLength(500);
        }
    }

    public class RetirerModificationCommandValidator : AbstractValidator<RetirerModificationCommand>
    {
        public RetirerModificationCommandValidator()
        {
            RuleFor(x => x.CommandeId).NotEmpty();
            RuleFor(x => x.Motif).NotEmpty().MaximumLength(500);
        }
    }

    public class RepondreModificationCommandValidator : AbstractValidator<RepondreModificationCommand>
    {
        public RepondreModificationCommandValidator()
        {
            RuleFor(x => x.CommandeId).NotEmpty();
            RuleFor(x => x.Regularisation).IsInEnum();
            RuleFor(x => x.Motif).MaximumLength(500);
        }
    }

    public class ReponsesModificationCommandsHandler :
        IRequestHandler<DeciderModificationAdminCommand>,
        IRequestHandler<RetirerModificationCommand>,
        IRequestHandler<RepondreModificationCommand, RepondreModificationResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditService _audit;

        public ReponsesModificationCommandsHandler(IApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
        {
            _context = context;
            _currentUser = currentUser;
            _audit = audit;
        }

        public async Task Handle(DeciderModificationAdminCommand request, CancellationToken cancellationToken)
        {
            var proposition = await ModificationHelper.ChargerPropositionAsync(_context, request.CommandeId, cancellationToken);
            if (proposition.Statut != StatutVersionCommande.EnValidationAdmin)
                throw new ConflictException("Cette proposition n'attend pas la validation de l'Administrateur.");

            var avant = proposition.Statut.ToString();
            if (request.Valider)
            {
                proposition.Statut = StatutVersionCommande.EnAttenteClient;
                proposition.ValideParId = _currentUser.UtilisateurId;
            }
            else
            {
                proposition.Statut = StatutVersionCommande.Refusee;
                proposition.MotifRefus = request.Motif!.Trim();
                proposition.DateReponse = DateTime.UtcNow;
            }

            _audit.Enregistrer(request.Valider ? "ValidationModificationAdmin" : "RefusModificationAdmin", "Commande", request.CommandeId,
                new { Version = proposition.NumeroVersion, Statut = avant },
                new { Version = proposition.NumeroVersion, Statut = proposition.Statut.ToString(), proposition.MotifRefus });
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task Handle(RetirerModificationCommand request, CancellationToken cancellationToken)
        {
            var proposition = await ModificationHelper.ChargerPropositionAsync(_context, request.CommandeId, cancellationToken);
            var avant = proposition.Statut.ToString();
            proposition.Statut = StatutVersionCommande.Retiree;
            proposition.MotifRefus = request.Motif.Trim();
            proposition.DateReponse = DateTime.UtcNow;

            _audit.Enregistrer("RetraitModificationCommande", "Commande", request.CommandeId,
                new { Version = proposition.NumeroVersion, Statut = avant },
                new { Version = proposition.NumeroVersion, Statut = proposition.Statut.ToString(), proposition.MotifRefus });
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<RepondreModificationResult> Handle(RepondreModificationCommand request, CancellationToken cancellationToken)
        {
            var commande = await CommandeHelper.ChargerAvecControleAccesAsync(_context, _currentUser, request.CommandeId, cancellationToken);
            var proposition = await ModificationHelper.ChargerPropositionAsync(_context, commande.Id, cancellationToken);
            if (proposition.Statut != StatutVersionCommande.EnAttenteClient)
                throw new ConflictException("Cette proposition attend encore la validation de l'Administrateur.");

            if (!request.Accepter)
            {
                proposition.Statut = StatutVersionCommande.Refusee;
                proposition.MotifRefus = request.Motif?.Trim();
                proposition.DateReponse = DateTime.UtcNow;
                _audit.Enregistrer("RefusModificationClient", "Commande", commande.Id,
                    apres: new { Version = proposition.NumeroVersion, proposition.MotifRefus });
                await _context.SaveChangesAsync(cancellationToken);
                return new RepondreModificationResult(commande.Statut.ToString(), commande.VersionActive, 0, 0,
                    "Modification refusée : la commande continue avec la version actuelle.");
            }

            // La commande a pu avancer depuis la proposition (remise au livreur, annulation...)
            await ModificationHelper.VerifierModifiableAsync(_context, commande, cancellationToken);

            var ancienne = await _context.VersionsCommande
                .FirstAsync(v => v.CommandeId == commande.Id && v.NumeroVersion == commande.VersionActive, cancellationToken);
            var statutAvant = commande.Statut.ToString();

            // Bascule : la nouvelle version devient active, l'ancienne reste telle quelle (§20)
            ancienne.Active = false;
            proposition.Active = true;
            proposition.Statut = StatutVersionCommande.Acceptee;
            proposition.DateReponse = DateTime.UtcNow;
            commande.VersionActive = proposition.NumeroVersion;

            // Stock : on garde ce qui reste utile, on libère le surplus
            var besoins = proposition.Lignes.GroupBy(l => l.ProduitId).ToDictionary(g => g.Key, g => g.Sum(l => l.Quantite));
            var liberations = await StockCommande.LibererExcedentAsync(_context, commande, besoins, cancellationToken);

            // Argent : payé net comparé au nouveau total
            var payeNet = await SoldeCommande.PayeNetAsync(_context, commande.Id, cancellationToken);
            var reste = proposition.Total - payeNet;
            decimal aRendre = 0;
            string message;

            if (reste > 0)
            {
                // Le complément doit être payé et confirmé avant de poursuivre (§21)
                commande.Statut = StatutCommande.CommandeCreee;
                message = $"Version {proposition.NumeroVersion} acceptée : {reste:N0} FCFA restent à payer avant de reprendre la commande.";
            }
            else
            {
                if (reste < 0)
                {
                    aRendre = -reste;
                    proposition.Regularisation = request.Regularisation;
                    RegularisationFinanciere.CreerPourModification(_context, _audit, commande, proposition.Id, request.Regularisation, aRendre,
                        $"Baisse de prix : version {proposition.NumeroVersion}");
                }

                // Rien à payer : on réserve ce qui manque (commande en préparation => à reprendre)
                commande.Statut = StatutCommande.PaiementConfirme;
                var reserve = await ReservationStock.TenterAsync(_context, _audit, commande, cancellationToken);
                message = $"Version {proposition.NumeroVersion} acceptée. "
                    + (reserve ? "Le stock est réservé, la commande peut être (re)préparée." : "Le stock est insuffisant : la commande attend un réapprovisionnement.")
                    + (aRendre > 0 ? $" {aRendre:N0} FCFA seront rendus par {(request.Regularisation == ModeRegularisation.Avoir ? "avoir" : "remboursement")} après validation de l'Administrateur." : string.Empty);
            }

            _audit.Enregistrer("AcceptationModificationCommande", "Commande", commande.Id,
                new { Statut = statutAvant, Version = ancienne.NumeroVersion, ancienne.Total },
                new
                {
                    Statut = commande.Statut.ToString(),
                    Version = proposition.NumeroVersion,
                    proposition.Total,
                    PayeNet = payeNet,
                    ResteAPayer = Math.Max(reste, 0),
                    ARendre = aRendre,
                    Liberations = liberations
                });

            // xmin de la commande : pas de double acceptation, pas de collision avec une remise au livreur
            await _context.SaveChangesAsync(cancellationToken);

            return new RepondreModificationResult(commande.Statut.ToString(), commande.VersionActive, Math.Max(reste, 0), aRendre, message);
        }
    }
}
