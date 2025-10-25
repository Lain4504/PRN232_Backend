namespace AISAM.Services.IServices
{
    public interface IAdQuotaService
    {
        Task<bool> CheckCampaignQuotaAsync(Guid profileId);
        Task<bool> CheckBudgetQuotaAsync(Guid profileId, decimal requestedBudget);
        Task<(bool canCreate, string? errorMessage)> ValidateQuotaAsync(Guid profileId, decimal budget);
        Task<AdQuotaInfo> GetRemainingQuotaAsync(Guid profileId);
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
