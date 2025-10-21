using AISAM.Common.Dtos;
using AISAM.Data.Enumeration;

namespace AISAM.Services.IServices
{
    public interface ISubscriptionService
    {
        Task<SubscriptionDto> CreateSubscriptionAsync(Guid userId, SubscriptionPlanEnum plan);
        Task<SubscriptionDto> UpdateSubscriptionAsync(Guid userId, Guid subscriptionId, SubscriptionPlanEnum newPlan);
        Task CancelSubscriptionAsync(Guid userId, Guid subscriptionId);
        Task<SubscriptionDto?> GetActiveSubscriptionAsync(Guid userId);
        Task<List<SubscriptionDto>> GetUserSubscriptionsAsync(Guid userId);
        Task<bool> CheckQuotaAsync(Guid userId, string quotaType, int requestedAmount = 1);
        Task<bool> CheckSubscriptionPermissionsAsync(Guid userId);
        Task ActivateSubscriptionAsync(Guid subscriptionId);
        Task ExpireSubscriptionsAsync();
    }
}