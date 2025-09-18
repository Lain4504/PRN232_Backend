using BookStore.Services.IServices;

namespace BookStore.API.Services
{
    public class ScheduledPostProcessorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ScheduledPostProcessorService> _logger;
        private readonly TimeSpan _period = TimeSpan.FromMinutes(5); // Check every 5 minutes

        public ScheduledPostProcessorService(
            IServiceProvider serviceProvider,
            ILogger<ScheduledPostProcessorService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scheduled Post Processor Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var postService = scope.ServiceProvider.GetRequiredService<IPostService>();
                    
                    await postService.ProcessScheduledPostsAsync();
                    _logger.LogDebug("Processed scheduled posts at {Time}", DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing scheduled posts");
                }

                await Task.Delay(_period, stoppingToken);
            }

            _logger.LogInformation("Scheduled Post Processor Service stopped");
        }
    }
}