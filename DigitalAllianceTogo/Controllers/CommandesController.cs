using DigitalAllianceTogo.Application.Commandes.Commands.AnnulerCommande;
using DigitalAllianceTogo.Application.Commandes.Commands.CloturerCommande;
using DigitalAllianceTogo.Application.Commandes.Commands.Preparation;
using DigitalAllianceTogo.Application.Commandes.Commands.ReceptionnerRetour;
using DigitalAllianceTogo.Application.Commandes.Dtos;
using DigitalAllianceTogo.Application.Commandes.Queries.GetCommandeById;
using DigitalAllianceTogo.Application.Commandes.Queries.GetCommandes;
using DigitalAllianceTogo.Application.Common.Models;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Paiements.Commands.PayerAvecAvoir;
using DigitalAllianceTogo.Application.Paiements.Commands.SoumettrePaiement;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalAllianceTogo.Api.Controllers
{
    /// <summary>
    /// Commandes (§8-12). Un client ne voit et ne paie que ses propres commandes
    /// (vérifié dans les handlers). Rôles définis action par action.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class CommandesController : ControllerBase
    {
        private const string Lecture = Roles.Admin + "," + Roles.Commercial + "," + Roles.GestionnaireStock + "," + Roles.Client;
        private const string PersonnelOuClient = Roles.Admin + "," + Roles.Commercial + "," + Roles.Client;
        private const string GestionStock = Roles.Admin + "," + Roles.GestionnaireStock;
        private const string Personnel = Roles.Admin + "," + Roles.Commercial;

        private readonly ISender _sender;

        public CommandesController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>Liste paginée des commandes (un client ne voit que les siennes).</summary>
        [HttpGet]
        [Authorize(Roles = Lecture)]
        [ProducesResponseType(typeof(PaginatedList<CommandeDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedList<CommandeDto>>> GetCommandes([FromQuery] GetCommandesQuery query, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(query, cancellationToken));
        }

        /// <summary>Détail : lignes de la version active, paiements, date limite de paiement.</summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = Lecture)]
        [ProducesResponseType(typeof(CommandeDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CommandeDetailDto>> GetCommandeById(Guid id, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new GetCommandeByIdQuery(id), cancellationToken));
        }

        /// <summary>
        /// Paiement externe : le client transmet la référence de sa transaction
        /// (et éventuellement une capture). À vérifier ensuite par le Commercial.
        /// </summary>
        [HttpPost("{id:guid}/paiements")]
        [Authorize(Roles = PersonnelOuClient)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Guid>> SoumettrePaiement(Guid id, SoumettrePaiementCommand command, CancellationToken cancellationToken)
        {
            var paiementId = await _sender.Send(command with { CommandeId = id }, cancellationToken);
            return CreatedAtAction(nameof(GetCommandeById), new { id }, paiementId);
        }

        /// <summary>
        /// Règle tout ou partie du reste à payer avec un avoir disponible du client.
        /// Couvre tout : paiement confirmé et stock réservé ; sinon le reste se paie normalement.
        /// </summary>
        [HttpPost("{id:guid}/payer-avec-avoir")]
        [Authorize(Roles = PersonnelOuClient)]
        [ProducesResponseType(typeof(PayerAvecAvoirResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<PayerAvecAvoirResult>> PayerAvecAvoir(Guid id, PayerAvecAvoirCommand command, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(command with { CommandeId = id }, cancellationToken));
        }

        /// <summary>StockReserve → PreparationEnCours.</summary>
        [HttpPost("{id:guid}/demarrer-preparation")]
        [Authorize(Roles = GestionStock)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DemarrerPreparation(Guid id, CancellationToken cancellationToken)
        {
            await _sender.Send(new DemarrerPreparationCommand(id), cancellationToken);
            return NoContent();
        }

        /// <summary>PreparationEnCours → PretePourLivraison.</summary>
        [HttpPost("{id:guid}/terminer-preparation")]
        [Authorize(Roles = GestionStock)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> TerminerPreparation(Guid id, CancellationToken cancellationToken)
        {
            await _sender.Send(new TerminerPreparationCommand(id), cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Annulation selon le moment : avant paiement (clôturée aussitôt), après paiement
        /// (remboursement ou avoir), pendant la préparation (Annulation en cours, contrôle stock).
        /// </summary>
        [HttpPost("{id:guid}/annuler")]
        [Authorize(Roles = PersonnelOuClient)]
        [ProducesResponseType(typeof(AnnulerCommandeResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AnnulerCommandeResult>> Annuler(Guid id, AnnulerCommandeCommand command, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(command with { Id = id }, cancellationToken));
        }

        /// <summary>
        /// Retour contrôlé (annulation en préparation ou refus de livraison) :
        /// les produits déclarés endommagés passent en défectueux, les autres redeviennent vendables.
        /// </summary>
        [HttpPost("{id:guid}/reception-retour")]
        [Authorize(Roles = GestionStock)]
        [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<List<object>>> ReceptionnerRetour(Guid id, ReceptionnerRetourCommand command, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(command with { CommandeId = id }, cancellationToken));
        }

        /// <summary>
        /// Clôture d'une commande livrée ; clôture exceptionnelle (Admin, motif) sinon.
        /// Refusée tant qu'un remboursement est en attente ou échoué.
        /// </summary>
        [HttpPost("{id:guid}/cloturer")]
        [Authorize(Roles = Personnel)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Cloturer(Guid id, CloturerCommandeCommand command, CancellationToken cancellationToken)
        {
            await _sender.Send(command with { Id = id }, cancellationToken);
            return NoContent();
        }
    }
}
