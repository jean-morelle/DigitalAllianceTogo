using DigitalAllianceTogo.Application.Common.Models;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Finance.Commands;
using DigitalAllianceTogo.Application.Finance.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalAllianceTogo.Api.Controllers
{
    /// <summary>
    /// Régularisation financière (§22-23). Les demandes naissent d'une annulation ou d'un
    /// refus de livraison ; l'Administrateur seul les valide et les exécute.
    /// Le client consulte l'état de ses remboursements et avoirs.
    /// </summary>
    [ApiController]
    [Route("api")]
    [Produces("application/json")]
    [Authorize]
    public class FinanceController : ControllerBase
    {
        private const string Lecture = Roles.Admin + "," + Roles.Commercial + "," + Roles.Client;

        private readonly ISender _sender;

        public FinanceController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("remboursements")]
        [Authorize(Roles = Lecture)]
        [ProducesResponseType(typeof(PaginatedList<RemboursementDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedList<RemboursementDto>>> GetRemboursements([FromQuery] GetRemboursementsQuery query, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(query, cancellationToken));
        }

        [HttpPost("remboursements/{id:guid}/valider")]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ValiderRemboursement(Guid id, CancellationToken cancellationToken)
        {
            await _sender.Send(new ValiderRemboursementCommand(id), cancellationToken);
            return NoContent();
        }

        /// <summary>Résultat du transfert : exécuté (référence) ou échoué (raison). Réexécutable après échec.</summary>
        [HttpPost("remboursements/{id:guid}/executer")]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ExecuterRemboursement(Guid id, ExecuterRemboursementCommand command, CancellationToken cancellationToken)
        {
            await _sender.Send(command with { Id = id }, cancellationToken);
            return NoContent();
        }

        [HttpGet("avoirs")]
        [Authorize(Roles = Lecture)]
        [ProducesResponseType(typeof(PaginatedList<AvoirDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedList<AvoirDto>>> GetAvoirs([FromQuery] GetAvoirsQuery query, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(query, cancellationToken));
        }

        /// <summary>Valide l'avoir : il devient disponible pour le client.</summary>
        [HttpPost("avoirs/{id:guid}/valider")]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ValiderAvoir(Guid id, CancellationToken cancellationToken)
        {
            await _sender.Send(new ValiderAvoirCommand(id), cancellationToken);
            return NoContent();
        }

        [HttpPost("avoirs/{id:guid}/annuler")]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AnnulerAvoir(Guid id, AnnulerAvoirCommand command, CancellationToken cancellationToken)
        {
            await _sender.Send(command with { Id = id }, cancellationToken);
            return NoContent();
        }
    }
}
