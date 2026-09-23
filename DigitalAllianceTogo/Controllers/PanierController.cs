using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.PanierClient;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalAllianceTogo.Api.Controllers
{
    /// <summary>
    /// Panier du client connecté (§6) : ajouter, modifier, retirer, puis
    /// demander un devis ou commander au prix du catalogue.
    /// </summary>
    [ApiController]
    [Route("api/panier")]
    [Produces("application/json")]
    [Authorize(Roles = Roles.Client)]
    public class PanierController : ControllerBase
    {
        private readonly ISender _sender;

        public PanierController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PanierDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<PanierDto>> Get(CancellationToken cancellationToken) =>
            Ok(await _sender.Send(new GetPanierQuery(), cancellationToken));

        /// <summary>Fixe la quantité d'un produit (0 = le retirer) ; renvoie le panier à jour.</summary>
        [HttpPut("lignes/{produitId:guid}")]
        [ProducesResponseType(typeof(PanierDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<PanierDto>> DefinirQuantite(Guid produitId, [FromBody] QuantiteDto corps, CancellationToken cancellationToken) =>
            Ok(await _sender.Send(new DefinirQuantitePanierCommand(produitId, corps.Quantite), cancellationToken));

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Vider(CancellationToken cancellationToken)
        {
            await _sender.Send(new ViderPanierCommand(), cancellationToken);
            return NoContent();
        }

        /// <summary>Transforme le panier en demande de devis pour le Commercial.</summary>
        [HttpPost("demande-devis")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Guid>> DemanderDevis(DemanderDevisDepuisPanierCommand command, CancellationToken cancellationToken) =>
            Ok(await _sender.Send(command, cancellationToken));

        /// <summary>Commande directe au prix du catalogue ; le paiement suit.</summary>
        [HttpPost("commander")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Guid>> Commander(CommanderPanierCommand command, CancellationToken cancellationToken) =>
            Ok(await _sender.Send(command, cancellationToken));

        public record QuantiteDto(int Quantite);
    }
}
