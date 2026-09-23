using DigitalAllianceTogo.Application.Commandes.Commands.ExpirerCommandesImpayees;
using MediatR;

namespace DigitalAllianceTogo.Common
{
    /// <summary>
    /// Toutes les heures : annule les commandes restées impayées au-delà du délai
    /// paramétré (ParametresEntreprise.DelaiExpirationPaiementHeures).
    /// Les actions sont journalisées comme actions du SYSTÈME (sans utilisateur).
    /// </summary>
    public class ExpirationCommandesService : BackgroundService
    {
        private static readonly TimeSpan Intervalle = TimeSpan.FromHours(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ExpirationCommandesService> _logger;

        public ExpirationCommandesService(IServiceScopeFactory scopeFactory, ILogger<ExpirationCommandesService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Intervalle);
            do
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                    var annulees = await sender.Send(new ExpirerCommandesImpayeesCommand(), stoppingToken);
                    if (annulees > 0)
                        _logger.LogInformation("{Nombre} commande(s) impayée(s) annulée(s) automatiquement", annulees);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Un échec ponctuel (base indisponible...) ne doit pas arrêter la tâche
                    _logger.LogError(ex, "Échec de l'expiration des commandes impayées");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}
