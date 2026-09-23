using DigitalAllianceTogo.Application.Commandes.Common;
using DigitalAllianceTogo.Application.Common.Exceptions;
using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Finance.Common;
using DigitalAllianceTogo.Application.Stock.Common;
using DigitalAllianceTogo.Domain.Enum;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigitalAllianceTogo.Application.Commandes.Commands.AnnulerCommande
{
    /// <summary>
    /// Annulation selon le moment (§18) :
    /// - avant paiement : Annulée → Clôturée, sans conséquence financière ;
    /// - après paiement, avant préparation : réservation libérée, demande de remboursement
    ///   ou d'avoir, puis En attente de régularisation financière ;
    /// - pendant la préparation : Annulation en cours, le stock doit récupérer et contrôler
    ///   les produits (voir ReceptionnerRetour) ;
    /// - après remise au livreur : ce n'est plus une annulation mais un refus de livraison ;
    /// - après livraison : c'est un retour SAV.
    /// </summary>
    public record AnnulerCommandeCommand : IRequest<AnnulerCommandeResult>
    {
        public Guid Id { get; init; }
        public string Motif { get; init; } = string.Empty;

        /// <summary>Si la commande a été payée : remboursement (par défaut) ou avoir.</summary>
        public ModeRegularisation Regularisation { get; init; } = ModeRegularisation.Remboursement;
    }

    public record AnnulerCommandeResult(string Statut, decimal MontantARegulariser, string Message);

    public class AnnulerCommandeCommandValidator : AbstractValidator<AnnulerCommandeCommand>
    {
        public AnnulerCommandeCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Motif).NotEmpty().WithMessage("Indiquez le motif de l'annulation.").MaximumLength(500);
            RuleFor(x => x.Regularisation).IsInEnum();
        }
    }

    public class AnnulerCommandeCommandHandler : IRequestHandler<AnnulerCommandeCommand, AnnulerCommandeResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditService _audit;

        public AnnulerCommandeCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
        {
            _context = context;
            _currentUser = currentUser;
            _audit = audit;
        }

        public async Task<AnnulerCommandeResult> Handle(AnnulerCommandeCommand request, CancellationToken cancellationToken)
        {
            var commande = await CommandeHelper.ChargerAvecControleAccesAsync(_context, _currentUser, request.Id, cancellationToken);
            var motif = $"Annulation : {request.Motif.Trim()}";
            var avant = commande.Statut;
            decimal montant = 0;
            string message;
            // Attend un paiement mais a déjà reçu de l'argent (avoir, avant un complément après modification)
            var dejaPaye = await SoldeCommande.ADejaPayeAsync(_context, commande.Id, cancellationToken);

            switch (commande.Statut)
            {
                case StatutCommande.CommandeCreee when !dejaPaye:
                case StatutCommande.PaiementEchoue when !dejaPaye:
                    commande.Statut = StatutCommande.Annulee;
                    _audit.Enregistrer("Annulation", "Commande", commande.Id,
                        new { Statut = avant.ToString() }, new { Statut = commande.Statut.ToString(), Motif = motif });
                    RegularisationFinanciere.Cloturer(_audit, commande, "Annulée avant paiement");
                    message = "Commande annulée et clôturée (aucun paiement encaissé).";
                    break;

                case StatutCommande.PaiementEnAttente:
                    throw new ConflictException("Le paiement est en cours de vérification : attendez sa confirmation ou son rejet avant d'annuler.");

                case StatutCommande.CommandeCreee:
                case StatutCommande.PaiementEchoue:
                case StatutCommande.PaiementConfirme:
                case StatutCommande.EnAttenteDisponibilite:
                case StatutCommande.StockReserve:
                    var liberations = await StockCommande.LibererAsync(_context, commande, cancellationToken);
                    if (liberations.Count > 0)
                        _audit.Enregistrer("LiberationStock", "Commande", commande.Id, apres: new { Liberations = liberations });
                    montant = await RegularisationFinanciere.CreerAsync(_context, _audit, commande, request.Regularisation, motif, cancellationToken);
                    await RegularisationFinanciere.TerminerAnnulationAsync(_context, _audit, commande, cancellationToken);
                    message = $"Commande annulée, réservation libérée. {DescriptionRegularisation(request.Regularisation, montant)}";
                    break;

                case StatutCommande.PreparationEnCours:
                case StatutCommande.PretePourLivraison:
                    // Une livraison planifiée n'a pas encore fait sortir de stock : on l'arrête
                    var planifiees = await _context.Livraisons
                        .Where(l => l.CommandeId == commande.Id && l.Statut == StatutLivraison.Planifiee)
                        .ToListAsync(cancellationToken);
                    foreach (var livraison in planifiees)
                    {
                        livraison.Statut = StatutLivraison.Echouee;
                        livraison.MotifEchec = "Commande annulée avant la remise au livreur";
                        _audit.Enregistrer("AnnulationLivraison", "Livraison", livraison.Id, apres: new { Statut = livraison.Statut.ToString(), livraison.MotifEchec });
                    }

                    montant = await RegularisationFinanciere.CreerAsync(_context, _audit, commande, request.Regularisation, motif, cancellationToken);
                    commande.Statut = StatutCommande.AnnulationEnCours;
                    _audit.Enregistrer("DemandeAnnulation", "Commande", commande.Id,
                        new { Statut = avant.ToString() }, new { Statut = commande.Statut.ToString(), Motif = motif });
                    message = "Annulation en cours : le stock doit récupérer et contrôler les produits. " +
                              DescriptionRegularisation(request.Regularisation, montant);
                    break;

                case StatutCommande.EnTransit:
                    throw new ConflictException("Le colis est déjà chez le livreur : ce n'est plus une annulation mais un refus de livraison.");

                case StatutCommande.Livree:
                    throw new ConflictException("La commande est livrée : ce n'est plus une annulation mais un retour SAV.");

                default:
                    throw new ConflictException($"Une commande au statut {commande.Statut} ne peut pas être annulée.");
            }

            // xmin de la commande : pas d'annulation concurrente d'une confirmation de paiement
            await _context.SaveChangesAsync(cancellationToken);
            return new AnnulerCommandeResult(commande.Statut.ToString(), montant, message.Trim());
        }

        private static string DescriptionRegularisation(ModeRegularisation mode, decimal montant) => montant <= 0
            ? string.Empty
            : $"{(mode == ModeRegularisation.Avoir ? "Avoir" : "Remboursement")} de {montant:N0} FCFA en attente de validation par l'Administrateur.";
    }
}
