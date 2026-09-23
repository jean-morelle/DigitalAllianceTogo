using DigitalAllianceTogo.Application.Notifications;
using MediatR;

namespace DigitalAllianceTogo.Common
{
    /// <summary>Chaque minute : envoie par e-mail les notifications client en attente.</summary>
    public class EnvoiNotificationsService : BackgroundService
    {
        private static readonly TimeSpan Intervalle = TimeSpan.FromMinutes(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EnvoiNotificationsService> _logger;

        public EnvoiNotificationsService(IServiceScopeFactory scopeFactory, ILogger<EnvoiNotificationsService> logger)
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
                    var envoyees = await sender.Send(new EnvoyerNotificationsEmailCommand(), stoppingToken);
                    if (envoyees > 0)
                        _logger.LogInformation("{Nombre} notification(s) envoyée(s) par e-mail", envoyees);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Échec de l'envoi des notifications par e-mail");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}
