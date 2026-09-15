using DigitalAllianceTogo.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DigitalAllianceTogo.Common
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var (statusCode, title) = MapException(exception);

            if (statusCode == StatusCodes.Status500InternalServerError)
                _logger.LogError(exception, "Erreur non gérée sur {Path}", httpContext.Request.Path);

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Instance = httpContext.Request.Path
            };

            if (exception is ApplicationValidationException validationException)
                problemDetails.Extensions["errors"] = validationException.Errors;
            else
                problemDetails.Detail = exception.Message;

            httpContext.Response.StatusCode = statusCode;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }

        private static (int StatusCode, string Title) MapException(Exception exception) => exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Ressource introuvable"),
            ApplicationValidationException => (StatusCodes.Status400BadRequest, "Erreur de validation"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflit"),
            _ => (StatusCodes.Status500InternalServerError, "Une erreur interne s'est produite")
        };
    }
}

