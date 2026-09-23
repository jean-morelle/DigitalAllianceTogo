using DigitalAllianceTogo.Application.Common.Models;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Sav.Commands.CreerTicketSav;
using DigitalAllianceTogo.Application.Sav.Commands.DeciderSav;
using DigitalAllianceTogo.Application.Sav.Commands.Logistique;
using DigitalAllianceTogo.Application.Sav.Commands.Technique;
using DigitalAllianceTogo.Application.Sav.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalAllianceTogo.Api.Controllers
{
    /// <summary>
    /// Service après-vente (§24-27). Le SAV ne rouvre jamais la commande.
    /// Ouvert → diagnostic → En réparation (→ clôture technique) ou Décision commerciale
    /// → Remplacement en cours (livraison RemplacementSav) / Régularisation financière → Clôturé.
    /// </summary>
    [ApiController]
    [Route("api/sav")]
    [Produces("application/json")]
    [Authorize]
    public class SavController : ControllerBase
    {
        private const string Lecture = Roles.Admin + "," + Roles.Commercial + "," + Roles.Technicien + "," + Roles.GestionnaireStock + "," + Roles.Client;
        private const string Ouverture = Roles.Admin + "," + Roles.Commercial + "," + Roles.Client;
        private const string Technique = Roles.Admin + "," + Roles.Technicien;
        private const string Commercial = Roles.Admin + "," + Roles.Commercial;
        private const string GestionStock = Roles.Admin + "," + Roles.GestionnaireStock;

        private readonly ISender _sender;

        public SavController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>Tickets SAV (un client ne voit que les siens).</summary>
        [HttpGet]
        [Authorize(Roles = Lecture)]
        [ProducesResponseType(typeof(PaginatedList<TicketSavDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedList<TicketSavDto>>> GetTickets([FromQuery] GetTicketsSavQuery query, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(query, cancellationToken));
        }

        /// <summary>Détail : diagnostics, interventions, livraisons de remplacement, remboursement / avoir.</summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = Lecture)]
        [ProducesResponseType(typeof(TicketSavDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TicketSavDetailDto>> GetTicket(Guid id, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new GetTicketSavByIdQuery(id), cancellationToken));
        }

        /// <summary>Ouvre un ticket sur une ligne d'une commande livrée.</summary>
        [HttpPost]
        [Authorize(Roles = Ouverture)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Guid>> Creer(CreerTicketSavCommand command, CancellationToken cancellationToken)
        {
            var id = await _sender.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetTicket), new { id }, id);
        }

        /// <summary>Technicien : réparable → En réparation ; sinon → Décision commerciale.</summary>
        [HttpPost("{id:guid}/diagnostic")]
        [Authorize(Roles = Technique)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Diagnostiquer(Guid id, DiagnostiquerTicketCommand command, CancellationToken cancellationToken)
        {
            await _sender.Send(command with { Id = id }, cancellationToken);
            return NoContent();
        }

        /// <summary>Technicien : test réussi → clôture technique ; raté → Décision commerciale.</summary>
        [HttpPost("{id:guid}/reparation")]
        [Authorize(Roles = Technique)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> TerminerReparation(Guid id, TerminerReparationCommand command, CancellationToken cancellationToken)
        {
            await _sender.Send(command with { Id = id }, cancellationToken);
            return NoContent();
        }

        /// <summary>Commercial : remplacement, remboursement ou avoir (choix du client).</summary>
        [HttpPost("{id:guid}/decision")]
        [Authorize(Roles = Commercial)]
        [ProducesResponseType(typeof(DeciderSavResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<DeciderSavResult>> Decider(Guid id, DeciderSavCommand command, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(command with { Id = id }, cancellationToken));
        }

        /// <summary>Planifie la livraison du produit de remplacement (type RemplacementSav).</summary>
        [HttpPost("{id:guid}/livraisons")]
        [Authorize(Roles = GestionStock)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Guid>> PlanifierLivraison(Guid id, PlanifierLivraisonSavCommand command, CancellationToken cancellationToken)
        {
            var livraisonId = await _sender.Send(command with { TicketId = id }, cancellationToken);
            return CreatedAtAction(nameof(GetTicket), new { id }, livraisonId);
        }

        /// <summary>Stock : réception et contrôle de l'ancien produit (réutilisable / défectueux).</summary>
        [HttpPost("{id:guid}/ancien-produit")]
        [Authorize(Roles = GestionStock)]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<object>> ReceptionnerAncienProduit(Guid id, ReceptionnerAncienProduitCommand command, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(command with { TicketId = id }, cancellationToken));
        }
    }
}
