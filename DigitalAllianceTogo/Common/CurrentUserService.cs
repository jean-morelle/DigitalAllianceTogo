using DigitalAllianceTogo.Application.Common.Interfaces;
using System.Security.Claims;

namespace DigitalAllianceTogo.Common
{
    /// <summary>
    /// Seule implémentation de ICurrentUserService dans toute la solution — parce que
    /// c'est la seule couche qui a le droit de connaître HttpContext. Lit l'Id de
    /// l'utilisateur depuis les claims du token JWT une fois l'authentification branchée
    /// (étape 10). En attendant, retourne toujours "non authentifié" proprement.
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public bool EstAuthentifie => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        public Guid? UtilisateurId
        {
            get
            {
                var claim = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                return Guid.TryParse(claim, out var id) ? id : null;
            }
        }

        public string? AdresseIP => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        public bool EstDansRole(string role) => _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
    }
}
