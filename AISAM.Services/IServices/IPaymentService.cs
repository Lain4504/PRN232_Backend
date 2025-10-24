using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Common;

namespace AISAM.Services.IServices
{
    public interface IPaymentService
    {
        Task<GenericResponse<CreatePaymentIntentResponse>> CreatePaymentIntentAsync(CreatePaymentIntentRequest request, Guid userId);
        Task<GenericResponse<PaymentResponseDto>> ConfirmPaymentAsync(string paymentIntentId, Guid userId);
        Task<GenericResponse<SubscriptionResponseDto>> CreateSubscriptionAsync(CreateSubscriptionRequest request, Guid userId);
        Task<GenericResponse<bool>> CancelSubscriptionAsync(Guid subscriptionId, Guid userId);
        Task<GenericResponse<SubscriptionResponseDto>> GetSubscriptionAsync(Guid subscriptionId, Guid userId);
        Task<GenericResponse<IEnumerable<SubscriptionResponseDto>>> GetUserSubscriptionsAsync(Guid userId);
        Task<GenericResponse<bool>> HandleWebhookAsync(string json, string stripeSignature);
    }
}