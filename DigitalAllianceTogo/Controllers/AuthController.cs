using DigitalAllianceTogo.Application.Auth.Commands.Login;
using DigitalAllianceTogo.Application.Auth.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using DigitalAllianceTogo.Common;

namespace DigitalAllianceTogo.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [AllowAnonymous] // seul contrôleur accessible sans token, par définition
    public class AuthController : ControllerBase
    {
        private readonly ISender _sender;

        public AuthController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>Authentifie un utilisateur et renvoie un token JWT.</summary>
        [HttpPost("login")]
        [EnableRateLimiting(RateLimiting.Anonyme)] // freine la force brute sur les mots de passe
        [ProducesResponseType(typeof(LoginResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult<LoginResultDto>> Login(LoginCommand command, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(command, cancellationToken);
            return Ok(result);
        }
    }
}
