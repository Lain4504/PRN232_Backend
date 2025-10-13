using AISAM.Services.IServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AISAM.Services.Service
{
    public class NotificationCleanupService : BackgroundService
    {
        private readonly ILogger<NotificationCleanupService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private Timer? _timer;

        public NotificationCleanupService(
            ILogger<NotificationCleanupService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationCleanupService is starting.");

            // Schedule the cleanup to run every day at 00:00 (midnight)
            ScheduleNextCleanup();

            return Task.CompletedTask;
        }

        private void ScheduleNextCleanup()
        {
            var now = DateTime.UtcNow;
            var nextRun = now.Date.AddDays(1); // Tomorrow at 00:00

            var delay = nextRun - now;

            _logger.LogInformation($"Next notification cleanup scheduled for {nextRun} (in {delay.TotalHours:F1} hours)");

            _timer = new Timer(async _ => await PerformCleanupAsync(),
                null,
                delay,
                TimeSpan.FromDays(1)); // Repeat every 24 hours
        }

        private async Task PerformCleanupAsync()
        {
            try
            {
                _logger.LogInformation("Starting notification cleanup process...");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                    // Delete notifications older than 30 days
                    var deletedCount = await notificationService.DeleteOldNotificationsAsync(30);

                    _logger.LogInformation($"Notification cleanup completed. Deleted {deletedCount} old notifications.");
                }

                // Schedule the next cleanup
                ScheduleNextCleanup();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during notification cleanup");

                // Still schedule the next cleanup even if this one failed
                ScheduleNextCleanup();
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationCleanupService is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            await base.StopAsync(stoppingToken);
        }
    }
}
