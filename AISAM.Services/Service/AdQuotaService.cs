using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using Microsoft.Extensions.Logging;

namespace AISAM.Services.Service
{
    public class AdQuotaService : IAdQuotaService
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IAdCampaignRepository _adCampaignRepository;
        private readonly IPerformanceReportRepository _performanceReportRepository;
        private readonly ILogger<AdQuotaService> _logger;

        public AdQuotaService(
            ISubscriptionRepository subscriptionRepository,
            IAdCampaignRepository adCampaignRepository,
            IPerformanceReportRepository performanceReportRepository,
            ILogger<AdQuotaService> logger)
        {
            _subscriptionRepository = subscriptionRepository;
            _adCampaignRepository = adCampaignRepository;
            _performanceReportRepository = performanceReportRepository;
            _logger = logger;
        }

        public async Task<bool> CheckCampaignQuotaAsync(Guid profileId)
        {
            try
            {
                var subscription = await _subscriptionRepository.GetActiveByProfileIdAsync(profileId);
                if (subscription == null)
                {
                    _logger.LogWarning("No active subscription found for profile {ProfileId}", profileId);
                    return false;
                }

                var activeCampaigns = await _adCampaignRepository.CountActiveByProfileIdAsync(profileId);
                var canCreate = activeCampaigns < subscription.QuotaAdCampaigns;

                _logger.LogInformation("Profile {ProfileId} campaign quota: {Active}/{Max}", 
                    profileId, activeCampaigns, subscription.QuotaAdCampaigns);

                return canCreate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking campaign quota for profile {ProfileId}", profileId);
                return false;
            }
        }

        public async Task<bool> CheckBudgetQuotaAsync(Guid profileId, decimal requestedBudget)
        {
            try
            {
                var subscription = await _subscriptionRepository.GetActiveByProfileIdAsync(profileId);
                if (subscription == null)
                {
                    _logger.LogWarning("No active subscription found for profile {ProfileId}", profileId);
                    return false;
                }

                var currentMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var currentMonthEnd = currentMonthStart.AddMonths(1).AddDays(-1);

                var currentMonthSpend = await _performanceReportRepository.GetTotalSpendByProfileIdAsync(profileId, currentMonthStart, currentMonthEnd);
                var totalAfterRequest = currentMonthSpend + requestedBudget;

                var canSpend = totalAfterRequest <= subscription.QuotaAdBudgetMonthly;

                _logger.LogInformation("Profile {ProfileId} budget quota: {Current} + {Requested} = {Total}/{Max}", 
                    profileId, currentMonthSpend, requestedBudget, totalAfterRequest, subscription.QuotaAdBudgetMonthly);

                return canSpend;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking budget quota for profile {ProfileId}", profileId);
                return false;
            }
        }

        public async Task<(bool canCreate, string? errorMessage)> ValidateQuotaAsync(Guid profileId, decimal budget)
        {
            try
            {
                var subscription = await _subscriptionRepository.GetActiveByProfileIdAsync(profileId);
                if (subscription == null)
                {
                    return (false, "No active subscription found");
                }

                // Check campaign quota
                var activeCampaigns = await _adCampaignRepository.CountActiveByProfileIdAsync(profileId);
                if (activeCampaigns >= subscription.QuotaAdCampaigns)
                {
                    return (false, $"Campaign limit reached ({activeCampaigns}/{subscription.QuotaAdCampaigns})");
                }

                // Check budget quota
                var currentMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var currentMonthEnd = currentMonthStart.AddMonths(1).AddDays(-1);
                var currentMonthSpend = await _performanceReportRepository.GetTotalSpendByProfileIdAsync(profileId, currentMonthStart, currentMonthEnd);
                var totalAfterRequest = currentMonthSpend + budget;

                if (totalAfterRequest > subscription.QuotaAdBudgetMonthly)
                {
                    var remaining = subscription.QuotaAdBudgetMonthly - currentMonthSpend;
                    return (false, $"Budget limit exceeded. Remaining budget: ${remaining:F2}");
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating quota for profile {ProfileId}", profileId);
                return (false, "Error validating quota");
            }
        }

        public async Task<AdQuotaInfo> GetRemainingQuotaAsync(Guid profileId)
        {
            try
            {
                var subscription = await _subscriptionRepository.GetActiveByProfileIdAsync(profileId);
                if (subscription == null)
                {
                    _logger.LogWarning("No active subscription found for profile {ProfileId}", profileId);
                    return new AdQuotaInfo
                    {
                        ActiveCampaigns = 0,
                        MaxCampaigns = 0,
                        CurrentMonthSpend = 0,
                        MaxMonthlyBudget = 0
                    };
                }

                var activeCampaigns = await _adCampaignRepository.CountActiveByProfileIdAsync(profileId);
                
                var currentMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var currentMonthEnd = currentMonthStart.AddMonths(1).AddDays(-1);
                var currentMonthSpend = await _performanceReportRepository.GetTotalSpendByProfileIdAsync(profileId, currentMonthStart, currentMonthEnd);

                return new AdQuotaInfo
                {
                    ActiveCampaigns = activeCampaigns,
                    MaxCampaigns = subscription.QuotaAdCampaigns,
                    CurrentMonthSpend = currentMonthSpend,
                    MaxMonthlyBudget = subscription.QuotaAdBudgetMonthly
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting remaining quota for profile {ProfileId}", profileId);
                return new AdQuotaInfo
                {
                    ActiveCampaigns = 0,
                    MaxCampaigns = 0,
                    CurrentMonthSpend = 0,
                    MaxMonthlyBudget = 0
                };
            }
        }
    }
}
