using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Stock.Commands.CreerEntrepot;
using DigitalAllianceTogo.Application.Stock.Commands.EntreeStock;
using DigitalAllianceTogo.Application.Stock.Commands.SurplusFournisseur;
using DigitalAllianceTogo.Application.Stock.Commands.SeuilAlerte;
using DigitalAllianceTogo.Application.Stock.Queries.GetMouvements;
using DigitalAllianceTogo.Application.Common.Models;
using DigitalAllianceTogo.Application.Stock.Queries.GetStocks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalAllianceTogo.Api.Controllers
{
    /// <summary>
    /// Stock (§11) : entrepôts, réceptions et consultation.
    /// Les réservations / libérations ne se font jamais à la main : elles découlent des commandes.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize(Roles = GestionStock)]
    public class StocksController : ControllerBase
    {
        private const string GestionStock = Roles.Admin + "," + Roles.GestionnaireStock;
        private const string Consultation = GestionStock + "," + Roles.Commercial;

        private readonly ISender _sender;

        public StocksController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>État du stock par produit et entrepôt.</summary>
        [HttpGet]
        [Authorize(Roles = Consultation)]
        [ProducesResponseType(typeof(List<StockProduitDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<StockProduitDto>>> GetStocks([FromQuery] GetStocksQuery query, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(query, cancellationToken));
        }

        /// <summary>Réception de marchandise ; relance les commandes payées en attente de stock.</summary>
        [HttpPost("entrees")]
        [ProducesResponseType(typeof(EntreeStockResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EntreeStockResult>> EntreeStock(EntreeStockCommand command, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(command, cancellationToken));
        }

        /// <summary>Seuil d'alerte d'une ligne de stock (0 = pas d'alerte).</summary>
        [HttpPut("{id:guid}/seuil-alerte")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ModifierSeuilAlerte(Guid id, ModifierSeuilAlerteCommand command, CancellationToken cancellationToken)
        {
            await _sender.Send(command with { StockProduitId = id }, cancellationToken);
            return NoContent();
        }

        /// <summary>Historique des mouvements d'une ligne de stock (entrées, réservations, sorties, retours...).</summary>
        [HttpGet("{id:guid}/mouvements")]
        [Authorize(Roles = Consultation)]
        [ProducesResponseType(typeof(PaginatedList<MouvementStockDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedList<MouvementStockDto>>> GetMouvements(Guid id, [FromQuery] int pageNumber = 1, CancellationToken cancellationToken = default)
        {
            return Ok(await _sender.Send(new GetMouvementsStockQuery { StockProduitId = id, PageNumber = pageNumber }, cancellationToken));
        }

        /// <summary>Surplus fournisseur (§30), par défaut ceux qui attendent une décision.</summary>
        [HttpGet("ecarts")]
        [ProducesResponseType(typeof(List<EcartReceptionDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<EcartReceptionDto>>> GetEcarts([FromQuery] GetEcartsReceptionQuery query, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(query, cancellationToken));
        }

        /// <summary>Administrateur : intégrer le surplus au stock ou le retourner au fournisseur.</summary>
        [HttpPost("ecarts/{id:guid}/decision")]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<List<string>>> DeciderEcart(Guid id, DeciderEcartReceptionCommand command, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(command with { Id = id }, cancellationToken));
        }

        [HttpGet("entrepots")]
        [Authorize(Roles = Consultation)]
        [ProducesResponseType(typeof(List<EntrepotDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<EntrepotDto>>> GetEntrepots(CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new GetEntrepotsQuery(), cancellationToken));
        }

        [HttpPost("entrepots")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Guid>> CreerEntrepot(CreerEntrepotCommand command, CancellationToken cancellationToken)
        {
            var id = await _sender.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetEntrepots), null, id);
        }
    }
}
