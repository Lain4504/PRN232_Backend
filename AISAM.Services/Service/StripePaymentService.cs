using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Common;
using AISAM.Data.Model;
using AISAM.Data.Enumeration;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace AISAM.Services.Service
{
    public class StripePaymentService : IPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly IPaymentRepository _paymentRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IUserRepository _userRepository;

        public StripePaymentService(
            IConfiguration configuration,
            IPaymentRepository paymentRepository,
            ISubscriptionRepository subscriptionRepository,
            IUserRepository userRepository)
        {
            _configuration = configuration;
            _paymentRepository = paymentRepository;
            _subscriptionRepository = subscriptionRepository;
            _userRepository = userRepository;

            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        public async Task<GenericResponse<CreatePaymentIntentResponse>> CreatePaymentIntentAsync(CreatePaymentIntentRequest request, Guid userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return GenericResponse<CreatePaymentIntentResponse>.CreateError("User not found");
                }

                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(request.Amount * 100), // Convert to cents
                    Currency = request.Currency.ToLower(),
                    Description = request.Description ?? $"Subscription payment for {user.Email}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "user_id", userId.ToString() },
                        { "subscription_plan", request.SubscriptionPlanId.ToString() }
                    }
                };

                var service = new PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options);

                var response = new CreatePaymentIntentResponse
                {
                    ClientSecret = paymentIntent.ClientSecret,
                    PaymentIntentId = paymentIntent.Id,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    Status = paymentIntent.Status
                };

                return GenericResponse<CreatePaymentIntentResponse>.CreateSuccess(response, "Payment intent created successfully");
            }
            catch (StripeException ex)
            {
                return GenericResponse<CreatePaymentIntentResponse>.CreateError($"Stripe error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return GenericResponse<CreatePaymentIntentResponse>.CreateError($"Error creating payment intent: {ex.Message}");
            }
        }

        public async Task<GenericResponse<PaymentResponseDto>> ConfirmPaymentAsync(string paymentIntentId, Guid userId)
        {
            try
            {
                var service = new PaymentIntentService();
                var paymentIntent = await service.GetAsync(paymentIntentId);

                if (paymentIntent.Metadata.TryGetValue("user_id", out var metadataUserId) &&
                    Guid.Parse(metadataUserId) != userId)
                {
                    return GenericResponse<PaymentResponseDto>.CreateError("Unauthorized access to payment");
                }

                // Create payment record in database
                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Amount = (decimal)paymentIntent.Amount / 100, // Convert from cents
                    Currency = paymentIntent.Currency.ToUpper(),
                    Status = paymentIntent.Status == "succeeded" ? PaymentStatusEnum.Success : PaymentStatusEnum.Failed,
                    TransactionId = paymentIntent.Id,
                    PaymentMethod = "stripe",
                    CreatedAt = DateTime.UtcNow
                };

                var createdPayment = await _paymentRepository.CreateAsync(payment);

                var response = new PaymentResponseDto
                {
                    Id = createdPayment.Id,
                    UserId = createdPayment.UserId,
                    Amount = createdPayment.Amount,
                    Currency = createdPayment.Currency,
                    Status = createdPayment.Status,
                    TransactionId = createdPayment.TransactionId,
                    PaymentMethod = createdPayment.PaymentMethod,
                    CreatedAt = createdPayment.CreatedAt
                };

                return GenericResponse<PaymentResponseDto>.CreateSuccess(response, "Payment confirmed successfully");
            }
            catch (Exception ex)
            {
                return GenericResponse<PaymentResponseDto>.CreateError($"Error confirming payment: {ex.Message}");
            }
        }

        public async Task<GenericResponse<SubscriptionResponseDto>> CreateSubscriptionAsync(CreateSubscriptionRequest request, Guid userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return GenericResponse<SubscriptionResponseDto>.CreateError("User not found");
                }

                // Check if user already has an active subscription
                var existingSubscription = await _subscriptionRepository.GetActiveByUserIdAsync(userId);
                if (existingSubscription != null)
                {
                    return GenericResponse<SubscriptionResponseDto>.CreateError("User already has an active subscription");
                }

                // Get subscription plan details
                var (quotaPosts, quotaStorage, priceId, durationMonths) = GetPlanDetails(request.Plan);

                // Create Stripe customer if not exists
                var customerService = new CustomerService();
                var customers = await customerService.ListAsync(new CustomerListOptions
                {
                    Email = user.Email,
                    Limit = 1
                });

                string customerId;
                if (customers.Data.Count == 0)
                {
                    var customer = await customerService.CreateAsync(new CustomerCreateOptions
                    {
                        Email = user.Email,
                        Name = user.Email // You might want to store user name separately
                    });
                    customerId = customer.Id;
                }
                else
                {
                    customerId = customers.Data[0].Id;
                }

                // Create subscription in Stripe
                var subscriptionOptions = new SubscriptionCreateOptions
                {
                    Customer = customerId,
                    Items = new List<SubscriptionItemOptions>
                    {
                        new SubscriptionItemOptions
                        {
                            Price = priceId
                        }
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        { "user_id", userId.ToString() },
                        { "plan", ((int)request.Plan).ToString() }
                    }
                };

                var subscriptionService = new Stripe.SubscriptionService();
                var stripeSubscription = await subscriptionService.CreateAsync(subscriptionOptions);

                // Create subscription in database
                var subscription = new Data.Model.Subscription
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Plan = request.Plan,
                    QuotaPostsPerMonth = quotaPosts,
                    QuotaStorageGb = quotaStorage,
                    StartDate = DateTime.UtcNow.Date,
                    EndDate = request.IsRecurring ? null : DateTime.UtcNow.AddMonths(durationMonths).Date,
                    IsActive = stripeSubscription.Status == "active",
                    CreatedAt = DateTime.UtcNow
                };

                var createdSubscription = await _subscriptionRepository.CreateAsync(subscription);

                var response = new SubscriptionResponseDto
                {
                    Id = createdSubscription.Id,
                    UserId = createdSubscription.UserId,
                    Plan = createdSubscription.Plan,
                    QuotaPostsPerMonth = createdSubscription.QuotaPostsPerMonth,
                    QuotaStorageGb = createdSubscription.QuotaStorageGb,
                    StartDate = createdSubscription.StartDate,
                    EndDate = createdSubscription.EndDate,
                    IsActive = createdSubscription.IsActive,
                    CreatedAt = createdSubscription.CreatedAt,
                    StripeSubscriptionId = stripeSubscription.Id,
                    StripeCustomerId = customerId
                };

                return GenericResponse<SubscriptionResponseDto>.CreateSuccess(response, "Subscription created successfully");
            }
            catch (StripeException ex)
            {
                return GenericResponse<SubscriptionResponseDto>.CreateError($"Stripe error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return GenericResponse<SubscriptionResponseDto>.CreateError($"Error creating subscription: {ex.Message}");
            }
        }

        public async Task<GenericResponse<bool>> CancelSubscriptionAsync(Guid subscriptionId, Guid userId)
        {
            try
            {
                var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
                if (subscription == null || subscription.UserId != userId)
                {
                    return GenericResponse<bool>.CreateError("Subscription not found or access denied");
                }

                // Cancel in Stripe (you'll need to store Stripe subscription ID)
                // This is a simplified version - in production you'd store the Stripe subscription ID

                // Update database
                subscription.IsActive = false;
                subscription.EndDate = DateTime.UtcNow.Date;
                await _subscriptionRepository.UpdateAsync(subscription);

                return GenericResponse<bool>.CreateSuccess(true, "Subscription cancelled successfully");
            }
            catch (Exception ex)
            {
                return GenericResponse<bool>.CreateError($"Error cancelling subscription: {ex.Message}");
            }
        }

        public async Task<GenericResponse<SubscriptionResponseDto>> GetSubscriptionAsync(Guid subscriptionId, Guid userId)
        {
            try
            {
                var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
                if (subscription == null || subscription.UserId != userId)
                {
                    return GenericResponse<SubscriptionResponseDto>.CreateError("Subscription not found or access denied");
                }

                var response = new SubscriptionResponseDto
                {
                    Id = subscription.Id,
                    UserId = subscription.UserId,
                    Plan = subscription.Plan,
                    QuotaPostsPerMonth = subscription.QuotaPostsPerMonth,
                    QuotaStorageGb = subscription.QuotaStorageGb,
                    StartDate = subscription.StartDate,
                    EndDate = subscription.EndDate,
                    IsActive = subscription.IsActive,
                    CreatedAt = subscription.CreatedAt
                };

                return GenericResponse<SubscriptionResponseDto>.CreateSuccess(response, "Subscription retrieved successfully");
            }
            catch (Exception ex)
            {
                return GenericResponse<SubscriptionResponseDto>.CreateError($"Error retrieving subscription: {ex.Message}");
            }
        }

        public async Task<GenericResponse<IEnumerable<SubscriptionResponseDto>>> GetUserSubscriptionsAsync(Guid userId)
        {
            try
            {
                var subscriptions = await _subscriptionRepository.GetByUserIdAsync(userId);

                var responses = subscriptions.Select(subscription => new SubscriptionResponseDto
                {
                    Id = subscription.Id,
                    UserId = subscription.UserId,
                    Plan = subscription.Plan,
                    QuotaPostsPerMonth = subscription.QuotaPostsPerMonth,
                    QuotaStorageGb = subscription.QuotaStorageGb,
                    StartDate = subscription.StartDate,
                    EndDate = subscription.EndDate,
                    IsActive = subscription.IsActive,
                    CreatedAt = subscription.CreatedAt
                });

                return GenericResponse<IEnumerable<SubscriptionResponseDto>>.CreateSuccess(responses, "Subscriptions retrieved successfully");
            }
            catch (Exception ex)
            {
                return GenericResponse<IEnumerable<SubscriptionResponseDto>>.CreateError($"Error retrieving subscriptions: {ex.Message}");
            }
        }

        public async Task<GenericResponse<bool>> HandleWebhookAsync(string json, string stripeSignature)
        {
            try
            {
                var webhookSecret = _configuration["Stripe:WebhookSecret"];
                var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);

                switch (stripeEvent.Type)
                {
                    case "payment_intent.succeeded":
                        var paymentIntent = stripeEvent.Data.Object as Stripe.PaymentIntent;
                        if (paymentIntent != null)
                        {
                            await HandlePaymentIntentSucceeded(paymentIntent);
                        }
                        break;

                    case "customer.subscription.created":
                    case "customer.subscription.updated":
                        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
                        if (subscription != null)
                        {
                            await HandleSubscriptionUpdated(subscription);
                        }
                        break;

                    case "customer.subscription.deleted":
                        var deletedSubscription = stripeEvent.Data.Object as Stripe.Subscription;
                        if (deletedSubscription != null)
                        {
                            await HandleSubscriptionCancelled(deletedSubscription);
                        }
                        break;
                }

                return GenericResponse<bool>.CreateSuccess(true, "Webhook handled successfully");
            }
            catch (Exception ex)
            {
                return GenericResponse<bool>.CreateError($"Error handling webhook: {ex.Message}");
            }
        }

        private async Task HandlePaymentIntentSucceeded(PaymentIntent paymentIntent)
        {
            // Update payment status in database
            if (paymentIntent.Metadata.TryGetValue("user_id", out var userIdStr) &&
                Guid.TryParse(userIdStr, out var userId))
            {
                var payment = await _paymentRepository.GetByTransactionIdAsync(paymentIntent.Id);
                if (payment != null)
                {
                    payment.Status = PaymentStatusEnum.Success;
                    await _paymentRepository.UpdateAsync(payment);
                }
            }
        }

        private async Task HandleSubscriptionUpdated(Stripe.Subscription stripeSubscription)
        {
            if (stripeSubscription.Metadata.TryGetValue("user_id", out var userIdStr) &&
                Guid.TryParse(userIdStr, out var userId))
            {
                var subscription = await _subscriptionRepository.GetActiveByUserIdAsync(userId);
                if (subscription != null)
                {
                    subscription.IsActive = stripeSubscription.Status == "active";
                    await _subscriptionRepository.UpdateAsync(subscription);
                }
            }
        }

        private async Task HandleSubscriptionCancelled(Stripe.Subscription stripeSubscription)
        {
            if (stripeSubscription.Metadata.TryGetValue("user_id", out var userIdStr) &&
                Guid.TryParse(userIdStr, out var userId))
            {
                var subscription = await _subscriptionRepository.GetActiveByUserIdAsync(userId);
                if (subscription != null)
                {
                    subscription.IsActive = false;
                    subscription.EndDate = DateTime.UtcNow.Date;
                    await _subscriptionRepository.UpdateAsync(subscription);
                }
            }
        }

        private (int quotaPosts, int quotaStorage, string priceId, int durationMonths) GetPlanDetails(SubscriptionPlanEnum plan)
        {
            return plan switch
            {
                SubscriptionPlanEnum.Free => (100, 5, "price_free", 0),
                SubscriptionPlanEnum.Basic => (300, 25, "price_1SKhFVBQwyx2lNBeKBWdPKPqy0Ie0FryNNMbSEwUTjmdD2TOJXRjTapF64ThjUJJhcvbFwnnt77JUijDKHlkURYG00BOWqriQd", 1),
                SubscriptionPlanEnum.Pro => (-1, 100, "price_pro", 3),
                _ => (100, 5, "price_free", 0)
            };
        }
    }
}