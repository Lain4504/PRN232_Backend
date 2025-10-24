using AISAM.Data.Enumeration;

namespace AISAM.Services.IServices
{
    public interface ISubscriptionValidationService
    {
        Task<bool> HasActiveSubscriptionAsync(Guid userId);
        Task<bool> HasRequiredPlanAsync(Guid userId, SubscriptionPlanEnum minimumPlan);
        Task<bool> CanUseFeatureAsync(Guid userId, string featureName);
        Task<SubscriptionPlanEnum> GetUserPlanAsync(Guid userId);
        Task<int> GetRemainingQuotaAsync(Guid userId, string quotaType);
        Task<bool> CheckAndDecrementQuotaAsync(Guid userId, string quotaType, int amount = 1);
    }
}