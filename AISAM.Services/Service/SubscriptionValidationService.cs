using AISAM.Data.Enumeration;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;

namespace AISAM.Services.Service
{
    public class SubscriptionValidationService : ISubscriptionValidationService
    {
        private readonly ISubscriptionRepository _subscriptionRepository;

        public SubscriptionValidationService(ISubscriptionRepository subscriptionRepository)
        {
            _subscriptionRepository = subscriptionRepository;
        }

        public async Task<bool> HasActiveSubscriptionAsync(Guid profileId)
        {
            var subscription = await _subscriptionRepository.GetActiveByProfileIdAsync(profileId);
            return subscription != null;
        }

        public async Task<bool> HasRequiredPlanAsync(Guid profileId, SubscriptionPlanEnum minimumPlan)
        {
            var subscription = await _subscriptionRepository.GetActiveByProfileIdAsync(profileId);
            if (subscription == null) return false;

            return (int)subscription.Plan >= (int)minimumPlan;
        }

        public async Task<bool> CanUseFeatureAsync(Guid profileId, string featureName)
        {
            var subscription = await _subscriptionRepository.GetActiveByProfileIdAsync(profileId);

            // Free features (available to all users)
            var freeFeatures = new[] { "basic_content_creation", "facebook_posting", "basic_analytics" };
            if (freeFeatures.Contains(featureName))
                return true;

            // AI content generation requires any active subscription (Basic or Pro)
            if (featureName == "ai_content_generation")
            {
                return subscription != null && subscription.IsActive;
            }

            // All other features are free for now
            return true;
        }

        public async Task<SubscriptionPlanEnum> GetUserPlanAsync(Guid profileId)
        {
            var subscription = await _subscriptionRepository.GetActiveByProfileIdAsync(profileId);
            return subscription?.Plan ?? SubscriptionPlanEnum.Free;
        }

        public async Task<int> GetRemainingQuotaAsync(Guid profileId, string quotaType)
        {
            var subscription = await _subscriptionRepository.GetActiveByProfileIdAsync(profileId);

            if (subscription == null)
            {
                // Free tier limits
                return quotaType switch
                {
                    "posts_per_month" => 100,
                    "ai_generations_per_month" => 10,
                    "storage_gb" => 5,
                    _ => 0
                };
            }

            // For now, return plan-based quotas
            // In production, you'd track actual usage
            return quotaType switch
            {
                "posts_per_month" => subscription.Plan == SubscriptionPlanEnum.Premium ? -1 : subscription.QuotaPostsPerMonth,
                "ai_generations_per_month" => subscription.Plan == SubscriptionPlanEnum.Premium ? 1000 : 100,
                "storage_gb" => subscription.Plan == SubscriptionPlanEnum.Premium ? 20 : (subscription.Plan == SubscriptionPlanEnum.Plus ? 10 : 5),
                _ => 0
            };
        }

        public async Task<bool> CheckAndDecrementQuotaAsync(Guid profileId, string quotaType, int amount = 1)
        {
            // For now, just check if user has quota remaining
            // In production, you'd decrement actual usage counters
            var remaining = await GetRemainingQuotaAsync(profileId, quotaType);

            // -1 means unlimited (Pro plan)
            if (remaining == -1) return true;

            return remaining >= amount;
        }
    }
}