using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Parametres;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalAllianceTogo.Api.Controllers
{
    /// <summary>Paramètres de l'entreprise : seuils métier et numéros de paiement Mobile Money.</summary>
    [ApiController]
    [Route("api/parametres")]
    [Produces("application/json")]
    [Authorize(Roles = Roles.Admin)]
    public class ParametresController : ControllerBase
    {
        private readonly ISender _sender;

        public ParametresController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ParametresDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<ParametresDto>> Get(CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new GetParametresQuery(), cancellationToken));
        }

        /// <summary>Modifie les paramètres (tracé dans le journal d'audit).</summary>
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Modifier(ModifierParametresCommand command, CancellationToken cancellationToken)
        {
            await _sender.Send(command, cancellationToken);
            return NoContent();
        }

        /// <summary>Numéros Mobile Money et bénéficiaire, affichés au client au moment de payer.</summary>
        [HttpGet("paiement")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(InfosPaiementDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<InfosPaiementDto>> GetInfosPaiement(CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new GetInfosPaiementQuery(), cancellationToken));
        }
    }
}
