using DigitalAllianceTogo.Application.Commandes.Dtos;
using DigitalAllianceTogo.Application.Common.Models;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Paiements.Commands.ConfirmerPaiement;
using DigitalAllianceTogo.Application.Paiements.Commands.RejeterPaiement;
using DigitalAllianceTogo.Application.Paiements.Queries.GetPaiements;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalAllianceTogo.Api.Controllers
{
    /// <summary>
    /// Vérification des paiements externes par le personnel (§9).
    /// Le client soumet sa preuve via POST /api/commandes/{id}/paiements.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize(Roles = Personnel)]
    public class PaiementsController : ControllerBase
    {
        private const string Personnel = Roles.Admin + "," + Roles.Commercial;

        private readonly ISender _sender;

        public PaiementsController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>Paiements (par défaut : preuves en attente de vérification, plus anciennes d'abord).</summary>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedList<PaiementDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedList<PaiementDto>>> GetPaiements([FromQuery] GetPaiementsQuery query, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(query, cancellationToken));
        }

        /// <summary>Transaction vérifiée : paiement confirmé, puis réservation du stock.</summary>
        [HttpPost("{id:guid}/confirmer")]
        [ProducesResponseType(typeof(ConfirmerPaiementResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ConfirmerPaiementResult>> Confirmer(Guid id, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new ConfirmerPaiementCommand(id), cancellationToken));
        }

        /// <summary>Preuve refusée (motif obligatoire) : le client peut réessayer dans le délai.</summary>
        [HttpPost("{id:guid}/rejeter")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Rejeter(Guid id, RejeterPaiementCommand command, CancellationToken cancellationToken)
        {
            await _sender.Send(command with { Id = id }, cancellationToken);
            return NoContent();
        }
    }
}
