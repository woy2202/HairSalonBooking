namespace HairSalon.Booking.Api.Infrastructure
{
    public sealed class AppointmentStatusBackgroundService : BackgroundService
    {
        private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AppointmentStatusBackgroundService> _logger;

        public AppointmentStatusBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<AppointmentStatusBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await RefreshStatusesAsync(stoppingToken);
                await Task.Delay(RefreshInterval, stoppingToken);
            }
        }

        private async Task RefreshStatusesAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IAppointmentStatusService>();
                await service.RefreshExpiredAppointmentsAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nie udało się automatycznie odświeżyć statusów wizyt.");
            }
        }
    }
}
