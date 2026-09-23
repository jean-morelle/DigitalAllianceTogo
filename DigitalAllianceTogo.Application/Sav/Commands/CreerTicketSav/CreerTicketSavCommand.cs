using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Sav.Common;
using DigitalAllianceTogo.Domain.Enum;
using DigitalAllianceTogo.Domain.Models.SAV;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Sav.Commands.CreerTicketSav
{
    /// <summary>
    /// Ouvre un ticket SAV sur une ligne d'une commande livrée (ou déjà clôturée) — §24, §26.
    /// Créé par le Commercial quand le client le contacte, ou par le client lui-même.
    /// Le SAV est une entité à part : la commande n'est PAS rouverte.
    /// </summary>
    public record CreerTicketSavCommand : IRequest<Guid>
    {
        public Guid LigneCommandeId { get; init; }
        public int Quantite { get; init; } = 1;
        public string Motif { get; init; } = string.Empty;
    }

    public class CreerTicketSavCommandValidator : AbstractValidator<CreerTicketSavCommand>
    {
        public CreerTicketSavCommandValidator()
        {
            RuleFor(x => x.LigneCommandeId).NotEmpty();
            RuleFor(x => x.Quantite).GreaterThan(0);
            RuleFor(x => x.Motif).NotEmpty().WithMessage("Décrivez la panne ou le problème.").MaximumLength(1000);
        }
    }

    public class CreerTicketSavCommandHandler : IRequestHandler<CreerTicketSavCommand, Guid>
    {
        private static readonly StatutCommande[] StatutsApresLivraison = { StatutCommande.Livree, StatutCommande.Cloturee };

        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditService _audit;

        public CreerTicketSavCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
        {
            _context = context;
            _currentUser = currentUser;
            _audit = audit;
        }

        public async Task<Guid> Handle(CreerTicketSavCommand request, CancellationToken cancellationToken)
        {
            var ligne = await _context.LignesCommande
                .Include(l => l.Produit)
                .Include(l => l.VersionCommande).ThenInclude(v => v.Commande).ThenInclude(c => c.Client)
                .FirstOrDefaultAsync(l => l.Id == request.LigneCommandeId, cancellationToken)
                ?? throw new NotFoundException("LigneCommande", request.LigneCommandeId);
            var commande = ligne.VersionCommande.Commande;

            if (!SavHelper.VoitTousLesTickets(_currentUser) && commande.Client.UtilisateurId != _currentUser.UtilisateurId)
                throw new ForbiddenAccessException();

            // Une commande clôturée après livraison reste éligible ; une commande annulée ne l'a jamais été
            var livree = commande.Statut == StatutCommande.Livree
                || (commande.Statut == StatutCommande.Cloturee
                    && await _context.Livraisons.AnyAsync(l => l.CommandeId == commande.Id && l.TicketSAVId == null
                        && (l.Statut == StatutLivraison.Livree || l.Statut == StatutLivraison.LivreeAvecReserve), cancellationToken));
            if (!livree)
                throw new ConflictException("Le SAV ne concerne que les produits livrés. Avant livraison, il s'agit d'une annulation ou d'un refus.");

            if (ligne.VersionCommande.NumeroVersion != commande.VersionActive)
                throw new ConflictException("Cette ligne appartient à une ancienne version de la commande.");

            var dejaEnSav = await _context.TicketsSAV
                .Where(t => t.LigneCommandeId == ligne.Id && t.Statut != StatutSav.Cloture)
                .SumAsync(t => t.Quantite, cancellationToken);
            if (dejaEnSav + request.Quantite > ligne.Quantite)
                throw new ConflictException($"{ligne.Quantite} unité(s) commandée(s), dont {dejaEnSav} déjà en SAV : impossible d'en ouvrir {request.Quantite} de plus.");

            var ticket = new TicketSAV
            {
                Id = Guid.NewGuid(),
                Reference = DigitalAllianceTogo.Application.Common.References.Generer("SAV"),
                DateCreation = DateTime.UtcNow,
                Motif = request.Motif.Trim(),
                Statut = StatutSav.Ouvert,
                Quantite = request.Quantite,
                ClientId = commande.ClientId,
                LigneCommandeId = ligne.Id
            };
            _context.TicketsSAV.Add(ticket);

            _audit.Enregistrer("OuvertureTicketSav", "TicketSAV", ticket.Id, apres: new
            {
                ticket.Reference,
                ticket.Motif,
                ticket.Quantite,
                Produit = ligne.Produit.Reference,
                Commande = commande.Reference,
                StatutCommande = commande.Statut.ToString() // inchangé : le SAV ne rouvre pas la commande
            });

            await _context.SaveChangesAsync(cancellationToken);
            return ticket.Id;
        }
    }
}
