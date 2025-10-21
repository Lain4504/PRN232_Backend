using AISAM.Services.IServices;
using Microsoft.Extensions.Logging;

namespace AISAM.Services.Service
{
    public class SubscriptionExpireJob
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILogger<SubscriptionExpireJob> _logger;

        public SubscriptionExpireJob(ISubscriptionService subscriptionService, ILogger<SubscriptionExpireJob> logger)
        {
            _subscriptionService = subscriptionService;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            try
            {
                _logger.LogInformation("Starting subscription expiration check");

                await _subscriptionService.ExpireSubscriptionsAsync();

                _logger.LogInformation("Subscription expiration check completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during subscription expiration check");
                throw;
            }
        }
    }
}