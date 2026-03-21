using AISAM.Data.Enumeration;

namespace AISAM.Common.Dtos.Request
{
    public class SeedDemoUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public SubscriptionPlanEnum Plan { get; set; } = SubscriptionPlanEnum.Free;
        public ProfileTypeEnum ProfileType { get; set; } = ProfileTypeEnum.Free;
        public string? CompanyName { get; set; }
        public bool CreateOrder { get; set; } = true;
        public decimal Amount { get; set; } = 0;
        public DateTime? CreatedAt { get; set; }
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
    }
}