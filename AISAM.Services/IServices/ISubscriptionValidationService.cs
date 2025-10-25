using AISAM.Data.Enumeration;

namespace AISAM.Services.IServices
{
    public interface ISubscriptionValidationService
    {
        Task<bool> HasActiveSubscriptionAsync(Guid profileId);
        Task<bool> HasRequiredPlanAsync(Guid profileId, SubscriptionPlanEnum minimumPlan);
        Task<bool> CanUseFeatureAsync(Guid profileId, string featureName);
        Task<SubscriptionPlanEnum> GetUserPlanAsync(Guid profileId);
        Task<int> GetRemainingQuotaAsync(Guid profileId, string quotaType);
        Task<bool> CheckAndDecrementQuotaAsync(Guid profileId, string quotaType, int amount = 1);
    }
}