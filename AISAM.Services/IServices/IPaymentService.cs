using AISAM.Common.Dtos;
using AISAM.Data.Enumeration;

namespace AISAM.Services.IServices
{
    public interface IPaymentService
    {
        Task<PaymentOrderDto> CreateOrderAsync(Guid userId, SubscriptionPlanEnum plan);
        Task<bool> HandlePaymentSuccessAsync(string orderId);
        Task<bool> HandlePaymentFailureAsync(string orderId);
    }
}