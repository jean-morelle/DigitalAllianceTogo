using DigitalAllianceTogo.Application.Common.Models;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Livraisons.Commands.ConfirmerLivraison;
using DigitalAllianceTogo.Application.Livraisons.Commands.PlanifierLivraison;
using DigitalAllianceTogo.Application.Livraisons.Commands.RemettreAuLivreur;
using DigitalAllianceTogo.Application.Livraisons.Commands.SignalerEchecLivraison;
using DigitalAllianceTogo.Application.Livraisons.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalAllianceTogo.Api.Controllers
{
    /// <summary>
    /// Livraison (§12) : Planifiée → (remise au livreur = sortie de stock) En transit
    /// → Livrée / Livrée avec réserve, ou échec (retour au dépôt, relivraison possible).
    /// Un livreur n'agit que sur SES livraisons (vérifié dans les handlers).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class LivraisonsController : ControllerBase
    {
        private const string GestionStock = Roles.Admin + "," + Roles.GestionnaireStock;
        private const string Terrain = Roles.Admin + "," + Roles.GestionnaireStock + "," + Roles.Livreur;

        private readonly ISender _sender;

        public LivraisonsController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>Livraisons, les plus proches d'abord (un livreur ne voit que les siennes).</summary>
        [HttpGet]
        [Authorize(Roles = Terrain)]
        [ProducesResponseType(typeof(PaginatedList<LivraisonDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedList<LivraisonDto>>> GetLivraisons([FromQuery] GetLivraisonsQuery query, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(query, cancellationToken));
        }

        /// <summary>Détail : adresse, téléphone du destinataire, preuve.</summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = Terrain)]
        [ProducesResponseType(typeof(LivraisonDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<LivraisonDto>> GetLivraisonById(Guid id, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new GetLivraisonByIdQuery(id), cancellationToken));
        }

        /// <summary>Planifie la livraison d'une commande prête et l'assigne à un livreur.</summary>
        [HttpPost]
        [Authorize(Roles = GestionStock)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Guid>> Planifier(PlanifierLivraisonCommand command, CancellationToken cancellationToken)
        {
            var id = await _sender.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetLivraisonById), new { id }, id);
        }

        /// <summary>Colis remis au livreur : sortie de stock, commande En transit.</summary>
        [HttpPost("{id:guid}/remettre")]
        [Authorize(Roles = GestionStock)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RemettreAuLivreur(Guid id, CancellationToken cancellationToken)
        {
            await _sender.Send(new RemettreAuLivreurCommand(id), cancellationToken);
            return NoContent();
        }

        /// <summary>Colis remis au client, avec preuve (photo ou signature) et réserve éventuelle.</summary>
        [HttpPost("{id:guid}/livree")]
        [Authorize(Roles = Terrain)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ConfirmerLivraison(Guid id, ConfirmerLivraisonCommand command, CancellationToken cancellationToken)
        {
            await _sender.Send(command with { Id = id }, cancellationToken);
            return NoContent();
        }

        /// <summary>Échec : client absent (relivraison) ou refus du client.</summary>
        [HttpPost("{id:guid}/echec")]
        [Authorize(Roles = Terrain)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> SignalerEchec(Guid id, SignalerEchecLivraisonCommand command, CancellationToken cancellationToken)
        {
            await _sender.Send(command with { Id = id }, cancellationToken);
            return NoContent();
        }
    }
}
