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

        public NotificationCleanupService(
            ILogger<NotificationCleanupService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationCleanupService is starting.");

            // Calculate delay until next midnight
            var now = DateTime.UtcNow;
            var nextRun = now.Date.AddDays(1); // Tomorrow at 00:00
            var initialDelay = nextRun - now;

            _logger.LogInformation($"Next notification cleanup scheduled for {nextRun} (in {initialDelay.TotalHours:F1} hours)");

            // Wait for the initial delay
            await Task.Delay(initialDelay, stoppingToken);

            // Main loop - run cleanup every 24 hours
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformCleanupAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during notification cleanup");
                }

                // Wait 24 hours before next cleanup (unless cancelled)
                try
                {
                    await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
            }

            _logger.LogInformation("NotificationCleanupService is stopping.");
        }

        private async Task PerformCleanupAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting notification cleanup process...");

            using (var scope = _serviceProvider.CreateScope())
            {
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                // Delete notifications older than 30 days
                var deletedCount = await notificationService.DeleteOldNotificationsAsync(30);

                _logger.LogInformation($"Notification cleanup completed. Deleted {deletedCount} old notifications.");
            }
        }
    }
}
