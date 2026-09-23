using DigitalAllianceTogo.Application.Audit.Queries;
using DigitalAllianceTogo.Application.Common.Models;
using DigitalAllianceTogo.Application.Common.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalAllianceTogo.Api.Controllers
{
    /// <summary>
    /// Traçabilité (§32, §45) : QUI a fait QUOI, QUAND, AVANT / APRÈS.
    /// Lecture seule : le journal ne peut être ni modifié ni supprimé (application et base).
    /// </summary>
    [ApiController]
    [Route("api/audit")]
    [Produces("application/json")]
    [Authorize]
    public class AuditController : ControllerBase
    {
        private readonly ISender _sender;

        public AuditController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>Recherche dans le journal (entité, action, auteur, période, actions système).</summary>
        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType(typeof(PaginatedList<JournalAuditDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedList<JournalAuditDto>>> GetJournal([FromQuery] GetJournalAuditQuery query, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(query, cancellationToken));
        }

        /// <summary>
        /// Historique complet d'une commande : devis d'origine, paiements, stock, livraisons,
        /// remboursements, avoirs et SAV, dans l'ordre chronologique.
        /// </summary>
        [HttpGet("commandes/{id:guid}")]
        [Authorize(Roles = Roles.Admin + "," + Roles.Commercial)]
        [ProducesResponseType(typeof(List<JournalAuditDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<JournalAuditDto>>> GetHistoriqueCommande(Guid id, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new GetHistoriqueCommandeQuery(id), cancellationToken));
        }
    }
}
