using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.TableauDeBord.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalAllianceTogo.Api.Controllers
{
    /// <summary>Supervision et statistiques (phase 7).</summary>
    [ApiController]
    [Route("api/tableau-de-bord")]
    [Produces("application/json")]
    [Authorize]
    public class TableauDeBordController : ControllerBase
    {
        private const string Personnel = Roles.Admin + "," + Roles.Commercial + "," + Roles.GestionnaireStock + "," + Roles.Technicien;

        private readonly ISender _sender;

        public TableauDeBordController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// Files de travail de l'utilisateur connecté (selon ses rôles ; tout pour l'Admin) :
        /// nombre d'éléments en attente et date du plus ancien.
        /// </summary>
        [HttpGet("a-traiter")]
        [Authorize(Roles = Personnel)]
        [ProducesResponseType(typeof(List<FileDeTravailDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<FileDeTravailDto>>> GetATraiter(CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new GetATraiterQuery(), cancellationToken));
        }

        /// <summary>
        /// Indicateurs sur une période (30 derniers jours par défaut) : ventes, devis,
        /// acquisition par réseau social, top produits, livraison, SAV, situation du stock.
        /// </summary>
        [HttpGet("statistiques")]
        [Authorize(Roles = Roles.Admin + "," + Roles.Commercial)]
        [ProducesResponseType(typeof(StatistiquesDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<StatistiquesDto>> GetStatistiques([FromQuery] GetStatistiquesQuery query, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(query, cancellationToken));
        }
    }
}
