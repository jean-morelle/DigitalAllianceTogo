using DigitalAllianceTogo.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DigitalAllianceTogo.Application.Common.Behaviours
{
    public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull // pas IRequest<TResponse> : exclurait les commandes sans retour (IRequest)
    {
        private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;
        private readonly ICurrentUserService _currentUserService;

        public LoggingBehaviour(
            ILogger<LoggingBehaviour<TRequest, TResponse>> logger,
            ICurrentUserService currentUserService)
        {
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var utilisateurId = _currentUserService.UtilisateurId?.ToString() ?? "anonyme";

            _logger.LogInformation("DigitalAllianceTogo Application : {Name} par utilisateur {UtilisateurId}", requestName, utilisateurId);

            return await next();
        }
    }

}
