using DigitalAllianceTogo.Application.Common.Models;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Devis.Commands.AccepterDevis;
using DigitalAllianceTogo.Application.Devis.Commands.CreerDevis;
using DigitalAllianceTogo.Application.Devis.Commands.EnvoyerDevis;
using DigitalAllianceTogo.Application.Devis.Commands.ModifierDevis;
using DigitalAllianceTogo.Application.Devis.Commands.RefuserValidationDevis;
using DigitalAllianceTogo.Application.Devis.Commands.RepondreDevis;
using DigitalAllianceTogo.Application.Devis.Commands.ValiderDevis;
using DigitalAllianceTogo.Application.Devis.Dtos;
using DigitalAllianceTogo.Application.Devis.Queries.GetDevis;
using DigitalAllianceTogo.Application.Devis.Queries.GetDevisById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalAllianceTogo.Api.Controllers
{
    /// <summary>
    /// Cycle de vie du devis (§7) :
    /// Brouillon → [ValidationInterne] → validé → Envoyé → Accepté (crée la commande) / Refusé / ModificationDemandee → Expiré.
    /// Les rôles sont vérifiés ici ; les règles fines (seuil de remise, devis d'un autre client) dans les handlers.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class DevisController : ControllerBase
    {
        private const string Personnel = Roles.Admin + "," + Roles.Commercial;
        private const string PersonnelOuClient = Roles.Admin + "," + Roles.Commercial + "," + Roles.Client;

        private readonly ISender _sender;

        public DevisController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>Liste paginée des devis (un client ne voit que les siens).</summary>
        [HttpGet]
        [Authorize(Roles = PersonnelOuClient)]
        [ProducesResponseType(typeof(PaginatedList<DevisDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedList<DevisDto>>> GetDevis([FromQuery] GetDevisQuery query, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(query, cancellationToken));
        }

        /// <summary>Détail d'un devis avec ses lignes.</summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = PersonnelOuClient)]
        [ProducesResponseType(typeof(DevisDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DevisDetailDto>> GetDevisById(Guid id, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new GetDevisByIdQuery(id), cancellationToken));
        }

        /// <summary>Crée un devis en brouillon (prix lus dans le catalogue).</summary>
        [HttpPost]
        [Authorize(Roles = Personnel)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Guid>> CreerDevis(CreerDevisCommand command, CancellationToken cancellationToken)
        {
            var id = await _sender.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetDevisById), new { id }, id);
        }

        /// <summary>Modifie un devis non envoyé (annule la validation interne).</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = Personnel)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ModifierDevis(Guid id, ModifierDevisCommand command, CancellationToken cancellationToken)
        {
            await _sender.Send(command with { Id = id }, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Validation interne. Remise ≤ seuil : validé. Remise &gt; seuil par un Commercial :
        /// soumis à l'Administrateur. Par l'Administrateur : validé.
        /// </summary>
        [HttpPost("{id:guid}/valider")]
        [Authorize(Roles = Personnel)]
        [ProducesResponseType(typeof(ValiderDevisResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ValiderDevisResult>> ValiderDevis(Guid id, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new ValiderDevisCommand(id), cancellationToken));
        }

        /// <summary>L'Administrateur refuse la remise exceptionnelle (retour en brouillon).</summary>
        [HttpPost("{id:guid}/refuser-validation")]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RefuserValidation(Guid id, RefuserValidationDevisCommand command, CancellationToken cancellationToken)
        {
            await _sender.Send(command with { Id = id }, cancellationToken);
            return NoContent();
        }

        /// <summary>Envoie au client un devis validé par l'entreprise.</summary>
        [HttpPost("{id:guid}/envoyer")]
        [Authorize(Roles = Personnel)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> EnvoyerDevis(Guid id, CancellationToken cancellationToken)
        {
            await _sender.Send(new EnvoyerDevisCommand(id), cancellationToken);
            return NoContent();
        }

        /// <summary>Le client accepte le devis : la commande est créée automatiquement.</summary>
        [HttpPost("{id:guid}/accepter")]
        [Authorize(Roles = PersonnelOuClient)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Guid>> AccepterDevis(Guid id, AccepterDevisCommand command, CancellationToken cancellationToken)
        {
            var commandeId = await _sender.Send(command with { Id = id }, cancellationToken);
            return Ok(commandeId);
        }

        /// <summary>Le client refuse le devis ou demande une modification.</summary>
        [HttpPost("{id:guid}/repondre")]
        [Authorize(Roles = PersonnelOuClient)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RepondreDevis(Guid id, RepondreDevisCommand command, CancellationToken cancellationToken)
        {
            await _sender.Send(command with { Id = id }, cancellationToken);
            return NoContent();
        }
    }
}
