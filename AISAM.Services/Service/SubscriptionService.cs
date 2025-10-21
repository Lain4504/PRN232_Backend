using AISAM.Common.Dtos;
using AISAM.Data.Enumeration;
using AISAM.Data.Model;
using AISAM.Repositories;
using AISAM.Repositories.IRepositories;
using AISAM.Services.Helper;
using AISAM.Services.IServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISAM.Services.Service
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IUserRepository _userRepository;
        private readonly ITeamMemberRepository _teamMemberRepository;
        private readonly RolePermissionConfig _rolePermissionConfig;
        private readonly ILogger<SubscriptionService> _logger;
        private readonly AisamContext _context;

        // Plan configurations
        private static readonly Dictionary<SubscriptionPlanEnum, (int posts, int storage, decimal price)> PlanConfigs = new()
        {
            { SubscriptionPlanEnum.Free, (100, 5, 0) },
            { SubscriptionPlanEnum.Basic, (500, 25, 9.99m) },
            { SubscriptionPlanEnum.Pro, (2000, 100, 29.99m) }
        };

        public SubscriptionService(
            ISubscriptionRepository subscriptionRepository,
            IPaymentRepository paymentRepository,
            INotificationRepository notificationRepository,
            IAuditLogRepository auditLogRepository,
            IUserRepository userRepository,
            ITeamMemberRepository teamMemberRepository,
            RolePermissionConfig rolePermissionConfig,
            AisamContext context,
            ILogger<SubscriptionService> logger)
        {
            _subscriptionRepository = subscriptionRepository;
            _paymentRepository = paymentRepository;
            _notificationRepository = notificationRepository;
            _auditLogRepository = auditLogRepository;
            _userRepository = userRepository;
            _teamMemberRepository = teamMemberRepository;
            _rolePermissionConfig = rolePermissionConfig;
            _context = context;
            _logger = logger;
        }

        public async Task<SubscriptionDto> CreateSubscriptionAsync(Guid userId, SubscriptionPlanEnum plan)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Check if user already has active subscription
                var existingActive = await _subscriptionRepository.GetActiveByUserIdAsync(userId);
                if (existingActive != null)
                {
                    throw new InvalidOperationException("User already has an active subscription");
                }

                // Check role permissions for plan
                var planConfig = PlanConfigs[plan];
                if (plan == SubscriptionPlanEnum.Pro)
                {
                    // Pro plan requires Vendor role
                    var user = await _userRepository.GetByIdAsync(userId);
                    if (user == null)
                    {
                        throw new InvalidOperationException("User not found");
                    }

                    if (user.Role != UserRoleEnum.Vendor && user.Role != UserRoleEnum.Admin)
                    {
                        throw new UnauthorizedAccessException("Pro plan requires Vendor role or Admin privileges");
                    }
                }

                var subscription = new Subscription
                {
                    UserId = userId,
                    Plan = plan,
                    QuotaPostsPerMonth = planConfig.posts,
                    QuotaStorageGb = planConfig.storage,
                    StartDate = DateTime.UtcNow.Date,
                    EndDate = plan == SubscriptionPlanEnum.Free ? null : DateTime.UtcNow.Date.AddMonths(1),
                    IsActive = plan == SubscriptionPlanEnum.Free // Free plan auto-activate
                };

                var created = await _subscriptionRepository.CreateAsync(subscription);

                // Send notification
                await SendNotificationAsync(userId, "subscription_pending", $"Subscription to {plan} plan created", created.Id);

                // Log audit
                await _auditLogRepository.CreateAsync(new AuditLog
                {
                    ActorId = userId,
                    ActionType = "CREATE_SUBSCRIPTION",
                    TargetTable = "subscriptions",
                    TargetId = created.Id,
                    NewValues = $"{{\"plan\":\"{plan}\",\"userId\":\"{userId}\"}}"
                });

                // If not free plan, create payment order
                if (plan != SubscriptionPlanEnum.Free)
                {
                    // Create payment record directly instead of using service to avoid circular dependency
                    var payment = new Payment
                    {
                        UserId = userId,
                        Amount = PlanConfigs[plan].price,
                        Status = PaymentStatusEnum.Pending,
                        TransactionId = Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper(),
                        PaymentMethod = "mock"
                    };
                    await _paymentRepository.CreateAsync(payment);
                    _logger.LogInformation("Created payment record for subscription {SubscriptionId}", created.Id);
                }

                // Commit transaction
                await transaction.CommitAsync();

                _logger.LogInformation("User {UserId} created subscription {SubscriptionId} for plan {Plan}", userId, created.Id, plan);

                return MapToDto(created);
            }
            catch (Exception ex)
            {
                // Rollback on any error
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to create subscription for user {UserId}, rolling back transaction", userId);
                throw;
            }
        }

        public async Task<SubscriptionDto> UpdateSubscriptionAsync(Guid userId, Guid subscriptionId, SubscriptionPlanEnum newPlan)
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
            if (subscription == null || subscription.UserId != userId)
            {
                throw new KeyNotFoundException("Subscription not found");
            }

            if (!subscription.IsActive)
            {
                throw new InvalidOperationException("Cannot update inactive subscription");
            }

            var oldPlan = subscription.Plan;
            var newConfig = PlanConfigs[newPlan];

            subscription.Plan = newPlan;
            subscription.QuotaPostsPerMonth = newConfig.posts;
            subscription.QuotaStorageGb = newConfig.storage;

            // If upgrading, extend end date or create payment
            if ((int)newPlan > (int)oldPlan)
            {
                // Upgrade - create payment for prorated amount
                var payment = new Payment
                {
                    UserId = userId,
                    Amount = PlanConfigs[newPlan].price,
                    Status = PaymentStatusEnum.Pending,
                    TransactionId = Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper(),
                    PaymentMethod = "mock"
                };
                await _paymentRepository.CreateAsync(payment);
                _logger.LogInformation("Created upgrade payment for subscription {SubscriptionId}", subscription.Id);
                // TODO: Handle prorated billing
            }
            else if ((int)newPlan < (int)oldPlan)
            {
                // Downgrade - check if mid-cycle
                if (subscription.EndDate.HasValue && subscription.EndDate.Value > DateTime.UtcNow.Date.AddDays(7))
                {
                    throw new InvalidOperationException("Cannot downgrade mid-cycle");
                }
            }

            var updated = await _subscriptionRepository.UpdateAsync(subscription);

            // Send notification
            await SendNotificationAsync(userId, "subscription_updated", $"Subscription updated to {newPlan} plan", updated.Id);

            // Log audit
            await _auditLogRepository.CreateAsync(new AuditLog
            {
                ActorId = userId,
                ActionType = "UPDATE_SUBSCRIPTION",
                TargetTable = "subscriptions",
                TargetId = updated.Id,
                OldValues = $"{{\"plan\":\"{oldPlan}\"}}",
                NewValues = $"{{\"plan\":\"{newPlan}\"}}"
            });

            _logger.LogInformation("User {UserId} updated subscription {SubscriptionId} from {OldPlan} to {NewPlan}", userId, subscriptionId, oldPlan, newPlan);

            return MapToDto(updated);
        }

        public async Task CancelSubscriptionAsync(Guid userId, Guid subscriptionId)
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
            if (subscription == null || subscription.UserId != userId)
            {
                throw new KeyNotFoundException("Subscription not found");
            }

            if (!subscription.IsActive)
            {
                throw new InvalidOperationException("Subscription is already canceled");
            }

            subscription.IsActive = false;
            subscription.EndDate = DateTime.UtcNow.Date;

            await _subscriptionRepository.UpdateAsync(subscription);

            // Send notification
            await SendNotificationAsync(userId, "subscription_canceled", "Subscription canceled", subscription.Id);

            // Log audit
            await _auditLogRepository.CreateAsync(new AuditLog
            {
                ActorId = userId,
                ActionType = "CANCEL_SUBSCRIPTION",
                TargetTable = "subscriptions",
                TargetId = subscription.Id,
                OldValues = $"{{\"isActive\":true}}",
                NewValues = $"{{\"isActive\":false}}"
            });

            _logger.LogInformation("User {UserId} canceled subscription {SubscriptionId}", userId, subscriptionId);
        }

        public async Task<SubscriptionDto?> GetActiveSubscriptionAsync(Guid userId)
        {
            var subscription = await _subscriptionRepository.GetActiveByUserIdAsync(userId);
            return subscription != null ? MapToDto(subscription) : null;
        }

        public async Task<List<SubscriptionDto>> GetUserSubscriptionsAsync(Guid userId)
        {
            var subscriptions = await _subscriptionRepository.GetByUserIdAsync(userId);
            return subscriptions.Select(MapToDto).ToList();
        }

        public async Task<bool> CheckQuotaAsync(Guid userId, string quotaType, int requestedAmount = 1)
        {
            var subscription = await _subscriptionRepository.GetActiveByUserIdAsync(userId);
            if (subscription == null)
            {
                // No active subscription, use free limits
                var freeConfig = PlanConfigs[SubscriptionPlanEnum.Free];
                return quotaType switch
                {
                    "posts" => requestedAmount <= freeConfig.posts,
                    "storage" => requestedAmount <= freeConfig.storage,
                    _ => false
                };
            }

            return quotaType switch
            {
                "posts" => requestedAmount <= subscription.QuotaPostsPerMonth,
                "storage" => requestedAmount <= subscription.QuotaStorageGb,
                _ => false
            };
        }

        public async Task<bool> CheckSubscriptionPermissionsAsync(Guid userId)
        {
            // Check if user has permission to manage subscriptions
            // Admin can manage any subscription
            // Vendor can manage their own subscriptions
            // Team members can manage subscriptions if they have the permission

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            // Admin can manage all subscriptions
            if (user.Role == UserRoleEnum.Admin)
            {
                return true;
            }

            // Vendor can manage their own subscriptions
            if (user.Role == UserRoleEnum.Vendor)
            {
                return true;
            }

            // Check if user is a team member with subscription management permission
            var teamMember = await _teamMemberRepository.GetByUserIdAsync(userId);
            if (teamMember != null)
            {
                // Check if team member has subscription management permission
                return _rolePermissionConfig.HasCustomPermission(teamMember.Permissions, "VIEW_SUBSCRIPTIONS");
            }

            // Regular users cannot manage subscriptions
            return false;
        }

        public async Task ActivateSubscriptionAsync(Guid subscriptionId)
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
            if (subscription == null)
            {
                throw new KeyNotFoundException("Subscription not found");
            }

            if (subscription.IsActive)
            {
                return; // Already active
            }

            subscription.IsActive = true;
            if (!subscription.EndDate.HasValue)
            {
                subscription.EndDate = DateTime.UtcNow.Date.AddMonths(1);
            }

            await _subscriptionRepository.UpdateAsync(subscription);

            // Send notification
            await SendNotificationAsync(subscription.UserId, "subscription_activated", $"Subscription to {subscription.Plan} plan activated", subscription.Id);

            _logger.LogInformation("Subscription {SubscriptionId} activated", subscriptionId);
        }

        public async Task ExpireSubscriptionsAsync()
        {
            var expiredSubscriptions = await _subscriptionRepository.GetExpiredSubscriptionsAsync();

            foreach (var subscription in expiredSubscriptions)
            {
                subscription.IsActive = false;
                await _subscriptionRepository.UpdateAsync(subscription);

                // Send notification
                await SendNotificationAsync(subscription.UserId, "subscription_expired", $"Subscription to {subscription.Plan} plan expired", subscription.Id);

                // Reset to free quotas
                var freeConfig = PlanConfigs[SubscriptionPlanEnum.Free];
                subscription.Plan = SubscriptionPlanEnum.Free;
                subscription.QuotaPostsPerMonth = freeConfig.posts;
                subscription.QuotaStorageGb = freeConfig.storage;

                await _subscriptionRepository.UpdateAsync(subscription);

                _logger.LogInformation("Subscription {SubscriptionId} expired", subscription.Id);
            }
        }

        private async Task SendNotificationAsync(Guid userId, string type, string message, Guid targetId)
        {
            var notificationType = type switch
            {
                "subscription_pending" => NotificationTypeEnum.SubscriptionPending,
                "subscription_activated" => NotificationTypeEnum.SubscriptionActivated,
                "subscription_updated" => NotificationTypeEnum.SubscriptionUpdated,
                "subscription_canceled" => NotificationTypeEnum.SubscriptionCanceled,
                "subscription_expired" => NotificationTypeEnum.SubscriptionExpired,
                "payment_success" => NotificationTypeEnum.PaymentSuccess,
                "payment_failed" => NotificationTypeEnum.PaymentFailed,
                "quota_exceeded" => NotificationTypeEnum.QuotaExceeded,
                _ => NotificationTypeEnum.SystemUpdate
            };

            var notification = new Notification
            {
                UserId = userId,
                Title = type.Replace("_", " ").ToUpper(),
                Message = message,
                Type = notificationType,
                TargetId = targetId,
                TargetType = "subscription"
            };

            await _notificationRepository.CreateAsync(notification);
        }

        private static SubscriptionDto MapToDto(Subscription subscription)
        {
            return new SubscriptionDto
            {
                Id = subscription.Id,
                UserId = subscription.UserId,
                Plan = subscription.Plan,
                QuotaPostsPerMonth = subscription.QuotaPostsPerMonth,
                QuotaStorageGb = subscription.QuotaStorageGb,
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate,
                IsActive = subscription.IsActive,
                CreatedAt = subscription.CreatedAt,
                UpdatedAt = subscription.UpdatedAt
            };
        }
    }
}