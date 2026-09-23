using DigitalAllianceTogo.Application.Clients.Commands.Adresses;
using DigitalAllianceTogo.Application.Clients.Commands.CreerClient;
using DigitalAllianceTogo.Application.Clients.Commands.InscrireClient;
using DigitalAllianceTogo.Application.Clients.Commands.ModifierClient;
using DigitalAllianceTogo.Application.Clients.Dtos;
using DigitalAllianceTogo.Application.Clients.Queries.GetClientById;
using DigitalAllianceTogo.Application.Clients.Queries.GetClients;
using DigitalAllianceTogo.Application.Common.Models;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DigitalAllianceTogo.Api.Controllers
{
    /// <summary>
    /// Clients : enregistrés par le Commercial (prospects WhatsApp, boutique...) ou inscrits
    /// eux-mêmes sur le site. Un client n'accède qu'à SA fiche et SES adresses.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class ClientsController : ControllerBase
    {
        private const string Personnel = Roles.Admin + "," + Roles.Commercial;
        private const string PersonnelOuClient = Roles.Admin + "," + Roles.Commercial + "," + Roles.Client;

        private readonly ISender _sender;

        public ClientsController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>Recherche de clients (nom, téléphone, email, code...).</summary>
        [HttpGet]
        [Authorize(Roles = Personnel)]
        [ProducesResponseType(typeof(PaginatedList<ClientDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedList<ClientDto>>> GetClients([FromQuery] GetClientsQuery query, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(query, cancellationToken));
        }

        /// <summary>Fiche du client connecté.</summary>
        [HttpGet("moi")]
        [Authorize(Roles = Roles.Client)]
        [ProducesResponseType(typeof(ClientDetailDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<ClientDetailDto>> GetMaFiche(CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new GetClientByIdQuery(null), cancellationToken));
        }

        /// <summary>Fiche client avec adresses et historique (devis, commandes).</summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = PersonnelOuClient)]
        [ProducesResponseType(typeof(ClientDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ClientDetailDto>> GetClient(Guid id, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new GetClientByIdQuery(id), cancellationToken));
        }

        /// <summary>Le Commercial enregistre un client (sans compte sur le site).</summary>
        [HttpPost]
        [Authorize(Roles = Personnel)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Guid>> CreerClient(CreerClientCommand command, CancellationToken cancellationToken)
        {
            var id = await _sender.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetClient), new { id }, id);
        }

        /// <summary>Inscription d'un client depuis le site (crée le compte et la fiche).</summary>
        [HttpPost("inscription")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimiting.Anonyme)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult<Guid>> Inscription(InscrireClientCommand command, CancellationToken cancellationToken)
        {
            var id = await _sender.Send(command, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, id);
        }

        /// <summary>Met à jour une fiche client.</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = PersonnelOuClient)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> ModifierClient(Guid id, ModifierClientCommand command, CancellationToken cancellationToken)
        {
            await _sender.Send(command with { Id = id }, cancellationToken);
            return NoContent();
        }

        /// <summary>Ajoute une adresse de livraison.</summary>
        [HttpPost("{id:guid}/adresses")]
        [Authorize(Roles = PersonnelOuClient)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        public async Task<ActionResult<Guid>> AjouterAdresse(Guid id, AjouterAdresseCommand command, CancellationToken cancellationToken)
        {
            var adresseId = await _sender.Send(command with { ClientId = id }, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, adresseId);
        }

        /// <summary>Modifie une adresse (les commandes passées gardent leur copie).</summary>
        [HttpPut("{id:guid}/adresses/{adresseId:guid}")]
        [Authorize(Roles = PersonnelOuClient)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> ModifierAdresse(Guid id, Guid adresseId, ModifierAdresseCommand command, CancellationToken cancellationToken)
        {
            await _sender.Send(command with { ClientId = id, AdresseId = adresseId }, cancellationToken);
            return NoContent();
        }

        /// <summary>Supprime une adresse (les commandes passées gardent leur copie).</summary>
        [HttpDelete("{id:guid}/adresses/{adresseId:guid}")]
        [Authorize(Roles = PersonnelOuClient)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> SupprimerAdresse(Guid id, Guid adresseId, CancellationToken cancellationToken)
        {
            await _sender.Send(new SupprimerAdresseCommand(id, adresseId), cancellationToken);
            return NoContent();
        }
    }
}
