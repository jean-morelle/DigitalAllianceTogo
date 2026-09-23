using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Finance.Common;
using DigitalAllianceTogo.Application.Sav.Common;
using DigitalAllianceTogo.Application.Stock.Common;
using DigitalAllianceTogo.Domain.Enum;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Sav.Commands.DeciderSav
{
    /// <summary>
    /// Le Commercial enregistre le choix du client pour un produit irréparable (§26-27) :
    /// - Remplacement : même produit, réservé pour le ticket (si le stock le permet) puis
    ///   livré par une livraison RemplacementSav ;
    /// - Remboursement / Avoir : demande à valider par l'Administrateur.
    /// La décision peut être changée tant que le remplacement n'est pas parti chez le livreur.
    /// </summary>
    public record DeciderSavCommand : IRequest<DeciderSavResult>
    {
        public Guid Id { get; init; }
        public DecisionSav Decision { get; init; }
    }

    public record DeciderSavResult(string Statut, bool StockReserve, decimal MontantARegulariser, string Message);

    public class DeciderSavCommandValidator : AbstractValidator<DeciderSavCommand>
    {
        public DeciderSavCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Decision).IsInEnum();
        }
    }

    public class DeciderSavCommandHandler : IRequestHandler<DeciderSavCommand, DeciderSavResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditService _audit;

        public DeciderSavCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
        {
            _context = context;
            _currentUser = currentUser;
            _audit = audit;
        }

        public async Task<DeciderSavResult> Handle(DeciderSavCommand request, CancellationToken cancellationToken)
        {
            var ticket = await SavHelper.ChargerAvecControleAccesAsync(_context, _currentUser, request.Id, cancellationToken);
            SavHelper.ExigerStatut(ticket, StatutSav.DecisionCommerciale, StatutSav.RemplacementEnCours);

            if (ticket.Statut == StatutSav.RemplacementEnCours)
            {
                if (request.Decision == DecisionSav.Remplacement)
                    throw new ConflictException("Le remplacement est déjà décidé.");
                if (await _context.Livraisons.AnyAsync(l => l.TicketSAVId == ticket.Id
                        && (l.Statut == StatutLivraison.Planifiee || l.Statut == StatutLivraison.EnTransit), cancellationToken))
                    throw new ConflictException("Une livraison de remplacement est en cours : la décision ne peut plus changer.");

                var liberations = await StockCommande.LibererAsync(_context, ticket, cancellationToken);
                if (liberations.Count > 0)
                    _audit.Enregistrer("LiberationStockSav", "TicketSAV", ticket.Id, apres: new { Liberations = liberations });
            }

            ticket.Decision = request.Decision;

            if (request.Decision == DecisionSav.Remplacement)
            {
                var reserve = await StockCommande.ReserverPourSavAsync(_context, ticket, ticket.LigneCommande.ProduitId, cancellationToken);
                SavHelper.ChangerStatut(_audit, ticket, StatutSav.RemplacementEnCours, "DecisionSav",
                    new { Decision = request.Decision.ToString(), StockReserve = reserve });
                await _context.SaveChangesAsync(cancellationToken);

                return new DeciderSavResult(ticket.Statut.ToString(), reserve, 0, reserve
                    ? "Remplacement décidé : le produit est réservé, la livraison peut être planifiée."
                    : "Remplacement décidé, mais le produit est en rupture : il sera réservé à la planification de la livraison.");
            }

            var mode = request.Decision == DecisionSav.Avoir ? ModeRegularisation.Avoir : ModeRegularisation.Remboursement;
            var montant = await RegularisationFinanciere.CreerPourSavAsync(_context, _audit, ticket, mode, cancellationToken);
            SavHelper.ChangerStatut(_audit, ticket, StatutSav.EnAttenteRegulationFinanciere, "DecisionSav",
                new { Decision = request.Decision.ToString(), Montant = montant });
            await _context.SaveChangesAsync(cancellationToken);

            return new DeciderSavResult(ticket.Statut.ToString(), false, montant,
                $"{(mode == ModeRegularisation.Avoir ? "Avoir" : "Remboursement")} de {montant:N0} FCFA en attente de validation par l'Administrateur.");
        }
    }
}
