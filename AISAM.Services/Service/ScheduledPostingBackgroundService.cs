using AISAM.Services.IServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AISAM.Services.Service
{
    public class ScheduledPostingBackgroundService : BackgroundService
    {
        private readonly IScheduledPostingService _scheduledPostingService;
        private readonly ILogger<ScheduledPostingBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Check every 5 minutes

        public ScheduledPostingBackgroundService(
            IScheduledPostingService scheduledPostingService,
            ILogger<ScheduledPostingBackgroundService> logger)
        {
            _scheduledPostingService = scheduledPostingService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scheduled Posting Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Checking for due schedules...");

                    // Process one-time schedules
                    await _scheduledPostingService.ProcessDueSchedulesAsync();

                    // Process recurring schedules
                    await _scheduledPostingService.ProcessRecurringSchedulesAsync();

                    _logger.LogInformation("Schedule check completed.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing scheduled posts");
                }

                // Wait for the next check interval or cancellation
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Scheduled Posting Background Service is stopping.");
        }
    }
}