namespace AISAM.Services.IServices
{
    public interface IAdQuotaService
    {
        Task<bool> CheckCampaignQuotaAsync(Guid userId);
        Task<bool> CheckBudgetQuotaAsync(Guid userId, decimal requestedBudget);
        Task<(bool canCreate, string? errorMessage)> ValidateQuotaAsync(Guid userId, decimal budget);
        Task<AdQuotaInfo> GetRemainingQuotaAsync(Guid userId);
    }

    public class AdQuotaInfo
    {
        public int ActiveCampaigns { get; set; }
        public int MaxCampaigns { get; set; }
        public decimal CurrentMonthSpend { get; set; }
        public decimal MaxMonthlyBudget { get; set; }
        public decimal RemainingBudget => Math.Max(0, MaxMonthlyBudget - CurrentMonthSpend);
        public int RemainingCampaigns => Math.Max(0, MaxCampaigns - ActiveCampaigns);
    }
}
