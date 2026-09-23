using DigitalAllianceTogo.Application.Commandes.Dtos;
using DigitalAllianceTogo.Application.Commandes.Queries.GetCommandeById;
using DigitalAllianceTogo.Application.Commandes.Queries.GetCommandes;
using DigitalAllianceTogo.Application.Common.Models;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Paiements.Commands.SoumettrePaiement;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalAllianceTogo.Api.Controllers
{
    /// <summary>
    /// Commandes (§8-9). Un client ne voit et ne paie que ses propres commandes
    /// (vérifié dans les handlers).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize(Roles = PersonnelOuClient)]
    public class CommandesController : ControllerBase
    {
        private const string PersonnelOuClient = Roles.Admin + "," + Roles.Commercial + "," + Roles.Client;

        private readonly ISender _sender;

        public CommandesController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>Liste paginée des commandes (un client ne voit que les siennes).</summary>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedList<CommandeDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedList<CommandeDto>>> GetCommandes([FromQuery] GetCommandesQuery query, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(query, cancellationToken));
        }

        /// <summary>Détail : lignes de la version active, paiements, date limite de paiement.</summary>
        [HttpGet("{id:guid}")]
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
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Guid>> SoumettrePaiement(Guid id, SoumettrePaiementCommand command, CancellationToken cancellationToken)
        {
            var paiementId = await _sender.Send(command with { CommandeId = id }, cancellationToken);
            return CreatedAtAction(nameof(GetCommandeById), new { id }, paiementId);
        }
    }
}
