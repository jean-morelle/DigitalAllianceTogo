using DigitalAllianceTogo.Application.Common.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DigitalAllianceTogo.Application.Common.Behaviours
{
    public class UnhandledExceptionBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull // pas IRequest<TResponse> : exclurait les commandes sans retour (IRequest)
    {
        private readonly ILogger<UnhandledExceptionBehaviour<TRequest, TResponse>> _logger;

        public UnhandledExceptionBehaviour(ILogger<UnhandledExceptionBehaviour<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            try
            {
                return await next();
            }
            // Les exceptions métier (validation, 404, 401, conflit) sont des réponses normales :
            // elles sont traduites par l'API, pas la peine de les logguer comme des erreurs.
            catch (Exception ex) when (ex is not (ValidationException or NotFoundException or UnauthorizedException or ConflictException))
            {
                // On ne loggue que le nom de la requête : son contenu peut contenir un mot de passe.
                var requestName = typeof(TRequest).Name;
                _logger.LogError(ex, "DigitalAllianceTogo Application : requête {Name} en erreur", requestName);
                throw;
            }
        }
    }
}
