using AISAM.Common.Dtos;
using AISAM.Data.Enumeration;
using AISAM.Data.Model;
using AISAM.Repositories;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISAM.Services.Service
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<PaymentService> _logger;
        private readonly AisamContext _context;

        // Mock payment configurations
        private static readonly Dictionary<SubscriptionPlanEnum, decimal> PlanPrices = new()
        {
            { SubscriptionPlanEnum.Free, 0 },
            { SubscriptionPlanEnum.Basic, 9.99m },
            { SubscriptionPlanEnum.Pro, 29.99m }
        };

        public PaymentService(
            IPaymentRepository paymentRepository,
            ISubscriptionRepository subscriptionRepository,
            INotificationRepository notificationRepository,
            AisamContext context,
            ILogger<PaymentService> logger)
        {
            _paymentRepository = paymentRepository;
            _subscriptionRepository = subscriptionRepository;
            _notificationRepository = notificationRepository;
            _context = context;
            _logger = logger;
        }

        public async Task<PaymentOrderDto> CreateOrderAsync(Guid userId, SubscriptionPlanEnum plan)
        {
            var amount = PlanPrices[plan];
            var orderId = Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper();

            // Create payment record
            var payment = new Payment
            {
                UserId = userId,
                Amount = amount,
                Status = PaymentStatusEnum.Pending,
                TransactionId = orderId,
                PaymentMethod = "mock"
            };

            await _paymentRepository.CreateAsync(payment);

            // Mock payment URL
            var paymentUrl = $"https://mock-payment.com/pay?order_id={orderId}&amount={amount}";

            _logger.LogInformation("Created payment order {OrderId} for user {UserId}, plan {Plan}, amount {Amount}", orderId, userId, plan, amount);

            return new PaymentOrderDto
            {
                OrderId = orderId,
                PaymentUrl = paymentUrl,
                Amount = amount,
                Currency = "USD"
            };
        }

        public async Task<bool> HandlePaymentSuccessAsync(string orderId)
        {
            var payment = await _paymentRepository.GetByOrderIdAsync(orderId);
            if (payment == null)
            {
                _logger.LogWarning("Payment not found for order {OrderId}", orderId);
                return false;
            }

            if (payment.Status == PaymentStatusEnum.Success)
            {
                return true; // Already processed
            }

            payment.Status = PaymentStatusEnum.Success;
            await _paymentRepository.UpdateAsync(payment);

            // Find and activate pending subscription (not active yet)
            // Use raw SQL to avoid any tracking issues
            var pendingSubscriptionId = await _context.Subscriptions
                .Where(s => s.UserId == payment.UserId && !s.IsActive && !s.IsDeleted)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();

            if (pendingSubscriptionId != Guid.Empty)
            {
                // Update subscription using repository method
                var subscriptionToUpdate = await _subscriptionRepository.GetByIdAsync(pendingSubscriptionId);
                if (subscriptionToUpdate != null)
                {
                    subscriptionToUpdate.IsActive = true;
                    subscriptionToUpdate.EndDate = DateTime.UtcNow.Date.AddMonths(1);
                    subscriptionToUpdate.UpdatedAt = DateTime.UtcNow;
                    await _subscriptionRepository.UpdateAsync(subscriptionToUpdate);
                }

                // Send subscription activated notification directly
                try
                {
                    var notification = new Notification
                    {
                        UserId = payment.UserId,
                        Title = "SUBSCRIPTION ACTIVATED",
                        Message = $"Subscription activated",
                        Type = NotificationTypeEnum.SubscriptionActivated,
                        TargetId = pendingSubscriptionId,
                        TargetType = "subscription"
                    };

                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send subscription activated notification");
                }

                _logger.LogInformation("Activated subscription {SubscriptionId} after payment success", pendingSubscriptionId);
            }
            else
            {
                _logger.LogWarning("No pending subscription found for user {UserId} after payment success", payment.UserId);
            }

            // Send success notification - but avoid using repository that might cause tracking conflicts
            try
            {
                var notificationType = NotificationTypeEnum.PaymentSuccess;
                var notification = new Notification
                {
                    UserId = payment.UserId,
                    Title = "PAYMENT SUCCESS",
                    Message = $"Payment of ${payment.Amount} completed successfully",
                    Type = notificationType,
                    TargetId = payment.Id,
                    TargetType = "payment"
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send payment success notification");
            }

            _logger.LogInformation("Payment {OrderId} marked as successful", orderId);
            return true;
        }

        public async Task<bool> HandlePaymentFailureAsync(string orderId)
        {
            var payment = await _paymentRepository.GetByOrderIdAsync(orderId);
            if (payment == null)
            {
                _logger.LogWarning("Payment not found for order {OrderId}", orderId);
                return false;
            }

            payment.Status = PaymentStatusEnum.Failed;
            await _paymentRepository.UpdateAsync(payment);

            // Send failure notification directly
            try
            {
                var notification = new Notification
                {
                    UserId = payment.UserId,
                    Title = "PAYMENT FAILED",
                    Message = $"Payment of ${payment.Amount} failed",
                    Type = NotificationTypeEnum.PaymentFailed,
                    TargetId = payment.Id,
                    TargetType = "payment"
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send payment failed notification");
            }

            _logger.LogInformation("Payment {OrderId} marked as failed", orderId);
            return true;
        }

        // Removed SendNotificationAsync method to avoid repository conflicts
    }
}