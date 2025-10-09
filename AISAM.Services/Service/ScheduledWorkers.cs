using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AISAM.Services.Service
{
    public class ScheduledPostingWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ScheduledPostingWorker> _logger;

        public ScheduledPostingWorker(IServiceScopeFactory scopeFactory, ILogger<ScheduledPostingWorker> logger)
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
                    var calendarRepository = scope.ServiceProvider.GetRequiredService<IContentCalendarRepository>();
                    var postService = scope.ServiceProvider.GetRequiredService<IPostService>();

                    var due = await calendarRepository.GetDueSchedulesAsync(DateTime.UtcNow, 20);
                    foreach (var item in due)
                    {
                        try
                        {
                            var ownerId = Guid.Empty;
                            _logger.LogWarning("Scheduled publishing skipped - model properties missing: {Id}", item.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Scheduled publish failed for {Id}", item.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ScheduledPostingWorker loop error");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    public class MetricsPullWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MetricsPullWorker> _logger;

        public MetricsPullWorker(IServiceScopeFactory scopeFactory, ILogger<MetricsPullWorker> logger)
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
                    var postRepository = scope.ServiceProvider.GetRequiredService<IPostRepository>();
                    var postService = scope.ServiceProvider.GetRequiredService<IPostService>();

                    // Pull last N posts; in real case, pick recent ones
                    var paged = await postRepository.GetPagedAsync(null, null, 1, 50);
                    foreach (var post in paged.Data.Where(p => !string.IsNullOrEmpty(p.ExternalPostId)))
                    {
                        try
                        {
                            var ownerId = post.Content.Brand.UserId;
                            await postService.PullReportsAsync(ownerId, post.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Metrics pull failed for post {PostId}", post.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MetricsPullWorker loop error");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}

