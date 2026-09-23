using System.Threading.RateLimiting;

namespace DigitalAllianceTogo.Common
{
    /// <summary>
    /// Limitation de débit des endpoints accessibles sans compte (login, inscription) :
    /// freine la force brute sur les mots de passe et les inscriptions en masse.
    /// </summary>
    public static class RateLimiting
    {
        public const string Anonyme = "anonyme";

        public static IServiceCollection AddLimitationDebit(this IServiceCollection services)
        {
            return services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // 10 requêtes par minute et par adresse IP
                options.AddPolicy(Anonyme, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "inconnue",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));
            });
        }
    }
}
