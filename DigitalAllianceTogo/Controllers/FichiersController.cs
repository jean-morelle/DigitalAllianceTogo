using DigitalAllianceTogo.Application.Common.Interfaces;
using DigitalAllianceTogo.Application.Common.Security;
using DigitalAllianceTogo.Application.Fichiers;
using DigitalAllianceTogo.Application.Fichiers.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalAllianceTogo.Api.Controllers
{
    /// <summary>
    /// Fichiers de preuve : captures de paiement, photos et signatures de livraison.
    /// Envoi par tout utilisateur connecté (client compris) ; lecture réservée au personnel.
    /// </summary>
    [ApiController]
    [Route("api/fichiers")]
    [Authorize]
    public class FichiersController : ControllerBase
    {
        private const string Personnel = Roles.Admin + "," + Roles.Commercial + "," + Roles.GestionnaireStock + "," + Roles.Livreur + "," + Roles.Technicien;

        private readonly ISender _sender;
        private readonly IStockageFichiers _stockage;

        public FichiersController(ISender sender, IStockageFichiers stockage)
        {
            _sender = sender;
            _stockage = stockage;
        }

        /// <summary>
        /// Envoie un fichier (JPEG, PNG, WebP ou PDF, 5 Mo maximum) et renvoie son lien,
        /// à reporter dans preuveUrl, photoUrl ou signatureUrl.
        /// </summary>
        [HttpPost("{categorie}")]
        [RequestSizeLimit(ReglesFichiers.TailleMaximale + 64 * 1024)] // + marge pour l'enveloppe multipart
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(FichierEnvoyeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<FichierEnvoyeDto>> Envoyer(string categorie, IFormFile fichier, CancellationToken cancellationToken)
        {
            await using var contenu = fichier.OpenReadStream();
            var resultat = await _sender.Send(new EnvoyerFichierCommand { Categorie = categorie, Taille = fichier.Length, Contenu = contenu }, cancellationToken);
            return Created(resultat.Url, resultat);
        }

        /// <summary>Lit un fichier envoyé (personnel uniquement : ce sont des données personnelles).</summary>
        [HttpGet("{categorie}/{nom}")]
        [Authorize(Roles = Personnel)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Lire(string categorie, string nom)
        {
            var type = ReglesFichiers.TypeDepuisNom(nom);
            if (type is null || !ReglesFichiers.EstNomValide(categorie, nom))
                return NotFound();

            var flux = _stockage.Ouvrir(categorie, nom);
            if (flux is null)
                return NotFound();

            // Pas d'interprétation du contenu par le navigateur au-delà du type déclaré
            Response.Headers.XContentTypeOptions = "nosniff";
            Response.Headers.CacheControl = "private, max-age=86400";
            return File(flux, type.ContentType);
        }
    }
}
