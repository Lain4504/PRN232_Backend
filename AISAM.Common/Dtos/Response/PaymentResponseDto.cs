using AISAM.Data.Enumeration;

namespace AISAM.Common.Dtos.Response
{
    public class PaymentResponseDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? SubscriptionId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public PaymentStatusEnum Status { get; set; }
        public string? PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
        public string? InvoiceUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}