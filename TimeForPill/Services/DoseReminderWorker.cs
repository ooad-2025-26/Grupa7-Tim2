namespace TimeForPill.Services
{
    public class DoseReminderWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DoseReminderWorker> _logger;

        public DoseReminderWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<DoseReminderWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var workflow =
                        scope.ServiceProvider.GetRequiredService<IDoseWorkflowService>();

                    await workflow.RefreshMissedDosesAsync();
                    await workflow.SendDueReminderEmailsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Greska tokom obrade podsjetnika za doze.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
