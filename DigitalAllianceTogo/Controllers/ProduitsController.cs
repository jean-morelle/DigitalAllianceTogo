using DigitalAllianceTogo.Application.Categories.Commands.CreerCategorie;
using DigitalAllianceTogo.Application.Categories.Queries.GetCategories;
using DigitalAllianceTogo.Application.Marques.Commands.CreerMarque;
using DigitalAllianceTogo.Application.Common.Models;
using DigitalAllianceTogo.Application.Marques.Queries.GetMarques;
using DigitalAllianceTogo.Application.Produits.Commands.AjouterAttribut;
using DigitalAllianceTogo.Application.Produits.Commands.AjouterImage;
using DigitalAllianceTogo.Application.Produits.Commands.CreerProduit;
using DigitalAllianceTogo.Application.Produits.Commands.ModifierProduit;
using DigitalAllianceTogo.Application.Produits.Commands.SupprimerAttribut;
using DigitalAllianceTogo.Application.Produits.Commands.SupprimerProduit;
using DigitalAllianceTogo.Application.Produits.Dtos;
using DigitalAllianceTogo.Application.Produits.Queries.GetProduitById;
using DigitalAllianceTogo.Application.Produits.Queries.GetProduits;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalAllianceTogo.Api.Controllers
{
    /// <summary>
    /// Lecture publique (catalogue consultable sans compte, comme sur n'importe
    /// quel site e-commerce), écriture réservée aux rôles Admin/Catalogue.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ProduitsController : ControllerBase
    {
        private readonly ISender _sender;

        public ProduitsController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>Liste paginée des produits, avec recherche et filtres.</summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PaginatedList<ProduitDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedList<ProduitDto>>> GetProduits(
            [FromQuery] GetProduitsQuery query,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(query, cancellationToken));
        }

        /// <summary>Détail d'un produit (avec images et attributs).</summary>
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ProduitDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProduitDetailDto>> GetProduit(Guid id, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new GetProduitByIdQuery(id), cancellationToken));
        }

        /// <summary>Crée un nouveau produit.</summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Catalogue")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Guid>> CreerProduit(CreerProduitCommand command, CancellationToken cancellationToken)
        {
            var id = await _sender.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetProduit), new { id }, id);
        }

        /// <summary>Modifie un produit existant.</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,Catalogue")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ModifierProduit(Guid id, ModifierProduitRequest request, CancellationToken cancellationToken)
        {
            await _sender.Send(new ModifierProduitCommand
            {
                Id = id,
                Nom = request.Nom,
                Description = request.Description,
                Prix = request.Prix,
                CategorieId = request.CategorieId,
                MarqueId = request.MarqueId,
                Actif = request.Actif
            }, cancellationToken);

            return NoContent();
        }

        /// <summary>Supprime un produit (échoue s'il a déjà du stock/des ventes — voir Remarks du Handler).</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SupprimerProduit(Guid id, CancellationToken cancellationToken)
        {
            await _sender.Send(new SupprimerProduitCommand(id), cancellationToken);
            return NoContent();
        }

        /// <summary>Ajoute une image au produit.</summary>
        [HttpPost("{id:guid}/images")]
        [Authorize(Roles = "Admin,Catalogue")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        public async Task<ActionResult<Guid>> AjouterImage(Guid id, AjouterImageRequest request, CancellationToken cancellationToken)
        {
            var imageId = await _sender.Send(new AjouterImageCommand
            {
                ProduitId = id,
                Url = request.Url,
                Ordre = request.Ordre,
                EstPrincipale = request.EstPrincipale
            }, cancellationToken);

            return CreatedAtAction(nameof(GetProduit), new { id }, imageId);
        }

        /// <summary>Supprime une image du produit.</summary>
        [HttpDelete("images/{imageId:guid}")]
        [Authorize(Roles = "Admin,Catalogue")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> SupprimerImage(Guid imageId, CancellationToken cancellationToken)
        {
            await _sender.Send(new SupprimerImageCommand(imageId), cancellationToken);
            return NoContent();
        }

        /// <summary>Ajoute un attribut (ex: "Couleur" = "Rouge") au produit.</summary>
        [HttpPost("{id:guid}/attributs")]
        [Authorize(Roles = "Admin,Catalogue")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        public async Task<ActionResult<Guid>> AjouterAttribut(Guid id, AjouterAttributRequest request, CancellationToken cancellationToken)
        {
            var attributId = await _sender.Send(new AjouterAttributCommand
            {
                ProduitId = id,
                Cle = request.Cle,
                Valeur = request.Valeur,
                Ordre = request.Ordre
            }, cancellationToken);

            return CreatedAtAction(nameof(GetProduit), new { id }, attributId);
        }

        /// <summary>Supprime un attribut du produit.</summary>
        [HttpDelete("attributs/{attributId:guid}")]
        [Authorize(Roles = "Admin,Catalogue")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> SupprimerAttribut(Guid attributId, CancellationToken cancellationToken)
        {
            await _sender.Send(new SupprimerAttributCommand(attributId), cancellationToken);
            return NoContent();
        }

        /// <summary>Liste des catégories actives (pour peupler un formulaire produit).</summary>
        [HttpGet("~/api/categories")]
        [AllowAnonymous]
        public async Task<ActionResult<List<CategorieDto>>> GetCategories(CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new GetCategoriesQuery(), cancellationToken));
        }

        /// <summary>Crée une catégorie.</summary>
        [HttpPost("~/api/categories")]
        [Authorize(Roles = "Admin,Catalogue")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Guid>> CreerCategorie(CreerCategorieCommand command, CancellationToken cancellationToken)
        {
            return StatusCode(StatusCodes.Status201Created, await _sender.Send(command, cancellationToken));
        }

        /// <summary>Crée une marque.</summary>
        [HttpPost("~/api/marques")]
        [Authorize(Roles = "Admin,Catalogue")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Guid>> CreerMarque(CreerMarqueCommand command, CancellationToken cancellationToken)
        {
            return StatusCode(StatusCodes.Status201Created, await _sender.Send(command, cancellationToken));
        }

        /// <summary>Liste des marques actives (pour peupler un formulaire produit).</summary>
        [HttpGet("~/api/marques")]
        [AllowAnonymous]
        public async Task<ActionResult<List<MarqueDto>>> GetMarques(CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new GetMarquesQuery(), cancellationToken));
        }
    }

    public record ModifierProduitRequest(string Nom, string Description, decimal Prix, Guid CategorieId, Guid MarqueId, bool Actif);
    public record AjouterImageRequest(string Url, int Ordre, bool EstPrincipale);
    public record AjouterAttributRequest(string Cle, string Valeur, int Ordre);
}
