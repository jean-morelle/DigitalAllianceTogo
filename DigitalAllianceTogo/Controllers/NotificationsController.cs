using DigitalAllianceTogo.Application.Common.Models;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Notifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalAllianceTogo.Api.Controllers
{
    /// <summary>Notifications du client connecté (avancement de ses devis, commandes, livraisons, SAV).</summary>
    [ApiController]
    [Route("api/notifications")]
    [Produces("application/json")]
    [Authorize(Roles = Roles.Client)]
    public class NotificationsController : ControllerBase
    {
        private readonly ISender _sender;

        public NotificationsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PaginatedList<NotificationDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedList<NotificationDto>>> Get([FromQuery] GetMesNotificationsQuery query, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(query, cancellationToken));
        }

        /// <summary>Nombre de notifications non lues (pastille de la cloche).</summary>
        [HttpGet("non-lues")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        public async Task<ActionResult<int>> NombreNonLues(CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new GetNombreNonLuesQuery(), cancellationToken));
        }

        [HttpPost("{id:guid}/lue")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> MarquerLue(Guid id, CancellationToken cancellationToken)
        {
            await _sender.Send(new MarquerLuesCommand(id), cancellationToken);
            return NoContent();
        }

        [HttpPost("tout-lire")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> ToutLire(CancellationToken cancellationToken)
        {
            await _sender.Send(new MarquerLuesCommand(null), cancellationToken);
            return NoContent();
        }
    }
}
