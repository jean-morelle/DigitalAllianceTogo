using DigitalAllianceTogo.Application.Common.Models;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Utilisateurs.Commands.AssignerRole;
using DigitalAllianceTogo.Application.Utilisateurs.Commands.CreateUtilisateur;
using DigitalAllianceTogo.Application.Utilisateurs.Commands.DeleteUtilisateur;
using DigitalAllianceTogo.Application.Utilisateurs.Commands.RetirerRole;
using DigitalAllianceTogo.Application.Utilisateurs.Commands.UpdateUtilisateur;
using DigitalAllianceTogo.Application.Utilisateurs.Dtos;
using DigitalAllianceTogo.Application.Utilisateurs.Queries.GetUtilisateurById;
using DigitalAllianceTogo.Application.Utilisateurs.Queries.GetUtilisateurs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalAllianceTogo.Controllers
{
    /// <summary>
    /// Gestion des comptes internes et des rôles : réservée à l'Administrateur (§4).
    /// (Sans cette restriction, un client connecté pourrait s'attribuer le rôle Admin.)
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = Roles.Admin)]
    public class UtilisateursController : ControllerBase
    {
        private readonly ISender _sender;

        public UtilisateursController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>Liste paginée des utilisateurs, avec recherche et filtre Actif.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedList<UtilisateurDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedList<UtilisateurDto>>> GetUtilisateurs(
            [FromQuery] GetUtilisateursQuery query,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>Détail d'un utilisateur par Id.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(UtilisateurDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UtilisateurDto>> GetUtilisateur(Guid id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetUtilisateurByIdQuery(id), cancellationToken);
            return Ok(result);
        }

        /// <summary>Crée un nouvel utilisateur.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Guid>> CreateUtilisateur(
            CreateUtilisateurCommand command,
            CancellationToken cancellationToken)
        {
            var id = await _sender.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetUtilisateur), new { id }, id);
        }

        /// <summary>Met à jour les informations modifiables d'un utilisateur.</summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateUtilisateur(
            Guid id,
            UpdateUtilisateurRequest request,
            CancellationToken cancellationToken)
        {
            var command = new UpdateUtilisateurCommand
            {
                Id = id,
                Nom = request.Nom,
                Prenom = request.Prenom,
                Telephone = request.Telephone,
                Actif = request.Actif
            };

            await _sender.Send(command, cancellationToken);
            return NoContent();
        }

        /// <summary>Supprime un utilisateur.</summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUtilisateur(Guid id, CancellationToken cancellationToken)
        {
            await _sender.Send(new DeleteUtilisateurCommand(id), cancellationToken);
            return NoContent();
        }

        /// <summary>Assigne un rôle à un utilisateur.</summary>
        [HttpPost("{id:guid}/roles/{roleId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AssignerRole(Guid id, Guid roleId, CancellationToken cancellationToken)
        {
            await _sender.Send(new AssignerRoleCommand(id, roleId), cancellationToken);
            return NoContent();
        }

        /// <summary>Retire un rôle d'un utilisateur.</summary>
        [HttpDelete("{id:guid}/roles/{roleId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RetirerRole(Guid id, Guid roleId, CancellationToken cancellationToken)
        {
            await _sender.Send(new RetirerRoleCommand(id, roleId), cancellationToken);
            return NoContent();
        }
    }

    /// <summary>Corps de requête pour PUT — pas d'Id ici, il vient de la route.</summary>
    public record UpdateUtilisateurRequest(string Nom, string Prenom, string Telephone, bool Actif);
}

