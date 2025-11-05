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
        private readonly IProfileRepository _profileRepository;

        public StripePaymentService(
            IConfiguration configuration,
            IPaymentRepository paymentRepository,
            ISubscriptionRepository subscriptionRepository,
            IUserRepository userRepository,
            IProfileRepository profileRepository)
        {
            _configuration = configuration;
            _paymentRepository = paymentRepository;
            _subscriptionRepository = subscriptionRepository;
            _userRepository = userRepository;
            _profileRepository = profileRepository;

            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        public async Task<GenericResponse<CreatePaymentIntentResponse>> CreatePaymentIntentAsync(CreatePaymentIntentRequest request, Guid userId, Guid profileId)
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
                        { "profile_id", profileId.ToString() },
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

                // Get user email for response
                var user = await _userRepository.GetByIdAsync(userId);
                
                var response = new PaymentResponseDto
                {
                    Id = createdPayment.Id,
                    UserId = createdPayment.UserId,
                    UserEmail = user?.Email,
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

                // Validate that the user owns the profile
                var profile = await _profileRepository.GetByIdAsync(request.ProfileId);
                if (profile == null || profile.UserId != userId)
                {
                    return GenericResponse<SubscriptionResponseDto>.CreateError("Profile not found or access denied");
                }

                // Check if profile already has an active subscription
                var existingSubscription = await _subscriptionRepository.GetActiveByProfileIdAsync(request.ProfileId);
                if (existingSubscription != null)
                {
                    return GenericResponse<SubscriptionResponseDto>.CreateError("Profile already has an active subscription");
                }

                // Handle Free plan (no Stripe integration needed)
                if (request.Plan == SubscriptionPlanEnum.Free)
                {
                    var (freeQuotaPosts, freeQuotaStorage, _, _) = GetPlanDetails(request.Plan);
                    
                    // Create free subscription in database
                    var freeSubscription = new Data.Model.Subscription
                    {
                        Id = Guid.NewGuid(),
                        ProfileId = request.ProfileId,
                        Plan = request.Plan,
                        QuotaPostsPerMonth = freeQuotaPosts,
                        QuotaStorageGb = freeQuotaStorage,
                        StartDate = DateTime.UtcNow.Date,
                        EndDate = null, // Free plan has no end date
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var freeCreatedSubscription = await _subscriptionRepository.CreateAsync(freeSubscription);

                    // Update profile status to Active and set ProfileType
                    var freeProfile = await _profileRepository.GetByIdAsync(request.ProfileId);
                    if (freeProfile != null)
                    {
                        freeProfile.Status = ProfileStatusEnum.Active;
                        freeProfile.SubscriptionId = freeCreatedSubscription.Id;
                        freeProfile.ProfileType = ProfileTypeEnum.Free; // Set ProfileType for Free plan
                        await _profileRepository.UpdateAsync(freeProfile);
                    }

                    var freeResponse = new SubscriptionResponseDto
                    {
                        Id = freeCreatedSubscription.Id,
                        ProfileId = freeCreatedSubscription.ProfileId,
                        Plan = freeCreatedSubscription.Plan,
                        QuotaPostsPerMonth = freeCreatedSubscription.QuotaPostsPerMonth,
                        QuotaStorageGb = freeCreatedSubscription.QuotaStorageGb,
                        StartDate = freeCreatedSubscription.StartDate,
                        EndDate = freeCreatedSubscription.EndDate,
                        IsActive = freeCreatedSubscription.IsActive,
                        CreatedAt = freeCreatedSubscription.CreatedAt
                    };

                    return GenericResponse<SubscriptionResponseDto>.CreateSuccess(freeResponse, "Free subscription created successfully");
                }

                // Get subscription plan details for paid plans
                var (quotaPosts, quotaStorage, priceId, durationMonths) = GetPlanDetails(request.Plan);

                // Validate payment method for paid plans
                if (string.IsNullOrEmpty(request.PaymentMethodId))
                {
                    return GenericResponse<SubscriptionResponseDto>.CreateError("Payment method is required for paid subscription plans");
                }

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

                // Attach payment method to customer if provided
                if (!string.IsNullOrEmpty(request.PaymentMethodId))
                {
                    var paymentMethodService = new PaymentMethodService();
                    var paymentMethod = await paymentMethodService.AttachAsync(request.PaymentMethodId, new PaymentMethodAttachOptions
                    {
                        Customer = customerId
                    });
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
                        { "profile_id", request.ProfileId.ToString() },
                        { "plan", ((int)request.Plan).ToString() }
                    }
                };

                // Set default payment method if provided
                if (!string.IsNullOrEmpty(request.PaymentMethodId))
                {
                    subscriptionOptions.DefaultPaymentMethod = request.PaymentMethodId;
                }

                var subscriptionService = new Stripe.SubscriptionService();
                var stripeSubscription = await subscriptionService.CreateAsync(subscriptionOptions);

                // Create subscription in database
                var subscription = new Data.Model.Subscription
                {
                    Id = Guid.NewGuid(),
                    ProfileId = request.ProfileId,
                    Plan = request.Plan,
                    QuotaPostsPerMonth = quotaPosts,
                    QuotaStorageGb = quotaStorage,
                    StartDate = DateTime.UtcNow.Date,
                    EndDate = request.IsRecurring ? null : DateTime.UtcNow.AddMonths(durationMonths).Date,
                    IsActive = stripeSubscription.Status == "active",
                    StripeSubscriptionId = stripeSubscription.Id,
                    StripeCustomerId = customerId,
                    CreatedAt = DateTime.UtcNow
                };

                var createdSubscription = await _subscriptionRepository.CreateAsync(subscription);

                // Create payment record for the subscription
                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    SubscriptionId = createdSubscription.Id,
                    Amount = GetPlanPrice(request.Plan),
                    Currency = "USD",
                    Status = PaymentStatusEnum.Success,
                    TransactionId = stripeSubscription.LatestInvoiceId,
                    PaymentMethod = "stripe",
                    CreatedAt = DateTime.UtcNow
                };

                await _paymentRepository.CreateAsync(payment);

                // Update profile status to Active and set correct ProfileType
                var paidProfile = await _profileRepository.GetByIdAsync(request.ProfileId);
                if (paidProfile != null)
                {
                    paidProfile.Status = ProfileStatusEnum.Active;
                    paidProfile.SubscriptionId = createdSubscription.Id;
                    paidProfile.ProfileType = (ProfileTypeEnum)(int)request.Plan; // Map SubscriptionPlan to ProfileType
                    await _profileRepository.UpdateAsync(paidProfile);
                }

                var response = new SubscriptionResponseDto
                {
                    Id = createdSubscription.Id,
                    ProfileId = createdSubscription.ProfileId,
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
                if (subscription == null)
                {
                    return GenericResponse<bool>.CreateError("Subscription not found");
                }

                // Verify user owns the profile that owns the subscription
                var profile = await _profileRepository.GetByIdAsync(subscription.ProfileId);
                if (profile == null || profile.UserId != userId)
                {
                    return GenericResponse<bool>.CreateError("Access denied");
                }

                // Cancel in Stripe if we have the subscription ID
                if (!string.IsNullOrEmpty(subscription.StripeSubscriptionId))
                {
                    var subscriptionService = new Stripe.SubscriptionService();
                    await subscriptionService.CancelAsync(subscription.StripeSubscriptionId);
                }

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
                if (subscription == null)
                {
                    return GenericResponse<SubscriptionResponseDto>.CreateError("Subscription not found");
                }

                // Verify user owns the profile that owns the subscription
                var profile = await _profileRepository.GetByIdAsync(subscription.ProfileId);
                if (profile == null || profile.UserId != userId)
                {
                    return GenericResponse<SubscriptionResponseDto>.CreateError("Access denied");
                }

                var response = new SubscriptionResponseDto
                {
                    Id = subscription.Id,
                    ProfileId = subscription.ProfileId,
                    Plan = subscription.Plan,
                    QuotaPostsPerMonth = subscription.QuotaPostsPerMonth,
                    QuotaStorageGb = subscription.QuotaStorageGb,
                    StartDate = subscription.StartDate,
                    EndDate = subscription.EndDate,
                    IsActive = subscription.IsActive,
                    CreatedAt = subscription.CreatedAt,
                    StripeSubscriptionId = subscription.StripeSubscriptionId,
                    StripeCustomerId = subscription.StripeCustomerId
                };

                return GenericResponse<SubscriptionResponseDto>.CreateSuccess(response, "Subscription retrieved successfully");
            }
            catch (Exception ex)
            {
                return GenericResponse<SubscriptionResponseDto>.CreateError($"Error retrieving subscription: {ex.Message}");
            }
        }

        public async Task<GenericResponse<SubscriptionResponseDto>> GetActiveSubscriptionByProfileIdAsync(Guid profileId, Guid userId)
        {
            try
            {
                // Verify user owns the profile
                var profile = await _profileRepository.GetByIdAsync(profileId);
                if (profile == null || profile.UserId != userId)
                {
                    return GenericResponse<SubscriptionResponseDto>.CreateError("Access denied");
                }

                // Try to get active subscription first
                var subscription = await _subscriptionRepository.GetActiveByProfileIdAsync(profileId);
                
                // If no active subscription, get the most recent subscription for this profile
                if (subscription == null)
                {
                    var allSubscriptions = await _subscriptionRepository.GetByProfileIdAsync(profileId);
                    subscription = allSubscriptions.FirstOrDefault(); // Already ordered by CreatedAt descending
                }

                // If still no subscription found, return null data but success response
                if (subscription == null)
                {
                    return GenericResponse<SubscriptionResponseDto>.CreateError("No subscription found for this profile");
                }

                var response = new SubscriptionResponseDto
                {
                    Id = subscription.Id,
                    ProfileId = subscription.ProfileId,
                    Plan = subscription.Plan,
                    QuotaPostsPerMonth = subscription.QuotaPostsPerMonth,
                    QuotaStorageGb = subscription.QuotaStorageGb,
                    StartDate = subscription.StartDate,
                    EndDate = subscription.EndDate,
                    IsActive = subscription.IsActive,
                    CreatedAt = subscription.CreatedAt,
                    StripeSubscriptionId = subscription.StripeSubscriptionId,
                    StripeCustomerId = subscription.StripeCustomerId
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
                    ProfileId = subscription.ProfileId,
                    Plan = subscription.Plan,
                    QuotaPostsPerMonth = subscription.QuotaPostsPerMonth,
                    QuotaStorageGb = subscription.QuotaStorageGb,
                    StartDate = subscription.StartDate,
                    EndDate = subscription.EndDate,
                    IsActive = subscription.IsActive,
                    CreatedAt = subscription.CreatedAt,
                    StripeSubscriptionId = subscription.StripeSubscriptionId,
                    StripeCustomerId = subscription.StripeCustomerId
                });

                return GenericResponse<IEnumerable<SubscriptionResponseDto>>.CreateSuccess(responses, "Subscriptions retrieved successfully");
            }
            catch (Exception ex)
            {
                return GenericResponse<IEnumerable<SubscriptionResponseDto>>.CreateError($"Error retrieving subscriptions: {ex.Message}");
            }
        }

        public async Task<GenericResponse<IEnumerable<PaymentResponseDto>>> GetUserPaymentHistoryAsync(Guid userId)
        {
            try
            {
                var payments = await _paymentRepository.GetUserPaymentHistoryAsync(userId);

                var responses = payments.Select(payment => new PaymentResponseDto
                {
                    Id = payment.Id,
                    UserId = payment.UserId,
                    UserEmail = payment.User?.Email,
                    SubscriptionId = payment.SubscriptionId,
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    Status = payment.Status,
                    PaymentMethod = payment.PaymentMethod,
                    TransactionId = payment.TransactionId,
                    InvoiceUrl = payment.InvoiceUrl,
                    CreatedAt = payment.CreatedAt
                });

                return GenericResponse<IEnumerable<PaymentResponseDto>>.CreateSuccess(responses, "Payment history retrieved successfully");
            }
            catch (Exception ex)
            {
                return GenericResponse<IEnumerable<PaymentResponseDto>>.CreateError($"Error retrieving payment history: {ex.Message}");
            }
        }

        public async Task<GenericResponse<SubscriptionResponseDto>> ChangePlanAsync(ChangePlanRequest request, Guid userId, Guid profileId)
        {
            try
            {
                // Get the active subscription for this profile
                var currentSubscription = await _subscriptionRepository.GetActiveByProfileIdAsync(profileId);
                if (currentSubscription == null)
                {
                    return GenericResponse<SubscriptionResponseDto>.CreateError("No active subscription found for this profile");
                }

                // Verify user owns the profile
                var profile = await _profileRepository.GetByIdAsync(profileId);
                if (profile == null || profile.UserId != userId)
                {
                    return GenericResponse<SubscriptionResponseDto>.CreateError("Access denied");
                }

                // Check if it's the same plan
                if (currentSubscription.Plan == request.PlanId)
                {
                    return GenericResponse<SubscriptionResponseDto>.CreateError("You are already on this plan");
                }

                // Get plan details
                var (quotaPosts, quotaStorage, priceId, _) = GetPlanDetails(request.PlanId);

                // If subscription has Stripe subscription ID, update it in Stripe
                if (!string.IsNullOrEmpty(currentSubscription.StripeSubscriptionId))
                {
                    var subscriptionService = new Stripe.SubscriptionService();
                    var stripeSubscription = await subscriptionService.GetAsync(currentSubscription.StripeSubscriptionId);

                    // Get the subscription item to update
                    if (stripeSubscription.Items.Data.Count > 0)
                    {
                        var subscriptionItemId = stripeSubscription.Items.Data[0].Id;

                        // Update subscription item with new price
                        var subscriptionItemService = new Stripe.SubscriptionItemService();
                        var updateOptions = new SubscriptionItemUpdateOptions
                        {
                            Price = priceId,
                            ProrationBehavior = request.Immediate ? "create_prorations" : "none"
                        };

                        await subscriptionItemService.UpdateAsync(subscriptionItemId, updateOptions);

                        // If not immediate, schedule the change at period end
                        if (!request.Immediate)
                        {
                            var subscriptionUpdateOptions = new SubscriptionUpdateOptions
                            {
                                Metadata = new Dictionary<string, string>
                                {
                                    { "scheduled_plan_change", ((int)request.PlanId).ToString() },
                                    { "scheduled_billing_cycle", request.BillingCycle }
                                }
                            };
                            await subscriptionService.UpdateAsync(currentSubscription.StripeSubscriptionId, subscriptionUpdateOptions);
                        }
                    }
                }

                // Update subscription in database
                if (request.Immediate)
                {
                    currentSubscription.Plan = request.PlanId;
                    currentSubscription.QuotaPostsPerMonth = quotaPosts;
                    currentSubscription.QuotaStorageGb = quotaStorage;
                    currentSubscription.UpdatedAt = DateTime.UtcNow;
                }
                // If not immediate, we'll keep the current plan but mark for change at period end
                // This would require additional tracking - for now, we'll do immediate change

                await _subscriptionRepository.UpdateAsync(currentSubscription);

                // Update profile type to match new plan
                if (profile != null)
                {
                    profile.ProfileType = (ProfileTypeEnum)(int)request.PlanId;
                    await _profileRepository.UpdateAsync(profile);
                }

                var response = new SubscriptionResponseDto
                {
                    Id = currentSubscription.Id,
                    ProfileId = currentSubscription.ProfileId,
                    Plan = currentSubscription.Plan,
                    QuotaPostsPerMonth = currentSubscription.QuotaPostsPerMonth,
                    QuotaStorageGb = currentSubscription.QuotaStorageGb,
                    StartDate = currentSubscription.StartDate,
                    EndDate = currentSubscription.EndDate,
                    IsActive = currentSubscription.IsActive,
                    CreatedAt = currentSubscription.CreatedAt,
                    StripeSubscriptionId = currentSubscription.StripeSubscriptionId,
                    StripeCustomerId = currentSubscription.StripeCustomerId
                };

                return GenericResponse<SubscriptionResponseDto>.CreateSuccess(response, "Subscription plan changed successfully");
            }
            catch (StripeException ex)
            {
                return GenericResponse<SubscriptionResponseDto>.CreateError($"Stripe error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return GenericResponse<SubscriptionResponseDto>.CreateError($"Error changing subscription plan: {ex.Message}");
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
            // Find subscription by Stripe subscription ID first (most reliable)
            var subscription = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(stripeSubscription.Id);
            if (subscription != null)
            {
                subscription.IsActive = stripeSubscription.Status == "active";
                await _subscriptionRepository.UpdateAsync(subscription);
            }
        }

        private async Task HandleSubscriptionCancelled(Stripe.Subscription stripeSubscription)
        {
            // Find subscription by Stripe subscription ID first (most reliable)
            var subscription = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(stripeSubscription.Id);
            if (subscription != null)
            {
                subscription.IsActive = false;
                subscription.EndDate = DateTime.UtcNow.Date;
                await _subscriptionRepository.UpdateAsync(subscription);
            }
        }

        private (int quotaPosts, int quotaStorage, string priceId, int durationMonths) GetPlanDetails(SubscriptionPlanEnum plan)
        {
            return plan switch
            {
                SubscriptionPlanEnum.Free => (100, 5, "price_free", 0),
                SubscriptionPlanEnum.Basic => (300, 25, "price_1SM3lZQzPon4zlatFv8vIvsi", 1),
                SubscriptionPlanEnum.Pro => (-1, 100, "price_1SM3mfQzPon4zlat0ffUKMpU", 3),
                _ => (100, 5, "price_free", 0)
            };
        }

        private decimal GetPlanPrice(SubscriptionPlanEnum plan)
        {
            return plan switch
            {
                SubscriptionPlanEnum.Free => 0,
                SubscriptionPlanEnum.Basic => 29,
                SubscriptionPlanEnum.Pro => 99,
                _ => 0
            };
        }

        // Admin methods
        public async Task<GenericResponse<IEnumerable<PaymentResponseDto>>> GetAllPaymentsAsync()
        {
            try
            {
                var payments = await _paymentRepository.GetAllPaymentsAsync();

                var responses = payments.Select(payment => new PaymentResponseDto
                {
                    Id = payment.Id,
                    UserId = payment.UserId,
                    UserEmail = payment.User?.Email,
                    SubscriptionId = payment.SubscriptionId,
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    Status = payment.Status,
                    PaymentMethod = payment.PaymentMethod,
                    TransactionId = payment.TransactionId,
                    InvoiceUrl = payment.InvoiceUrl,
                    CreatedAt = payment.CreatedAt
                });

                return GenericResponse<IEnumerable<PaymentResponseDto>>.CreateSuccess(responses, "All payments retrieved successfully");
            }
            catch (Exception ex)
            {
                return GenericResponse<IEnumerable<PaymentResponseDto>>.CreateError($"Error retrieving all payments: {ex.Message}");
            }
        }

        public async Task<GenericResponse<IEnumerable<SubscriptionResponseDto>>> GetAllSubscriptionsAsync()
        {
            try
            {
                var subscriptions = await _subscriptionRepository.GetAllSubscriptionsAsync();

                var responses = subscriptions.Select(subscription => new SubscriptionResponseDto
                {
                    Id = subscription.Id,
                    ProfileId = subscription.ProfileId,
                    Plan = subscription.Plan,
                    QuotaPostsPerMonth = subscription.QuotaPostsPerMonth,
                    QuotaStorageGb = subscription.QuotaStorageGb,
                    StartDate = subscription.StartDate,
                    EndDate = subscription.EndDate,
                    IsActive = subscription.IsActive,
                    CreatedAt = subscription.CreatedAt,
                    StripeSubscriptionId = subscription.StripeSubscriptionId,
                    StripeCustomerId = subscription.StripeCustomerId
                });

                return GenericResponse<IEnumerable<SubscriptionResponseDto>>.CreateSuccess(responses, "All subscriptions retrieved successfully");
            }
            catch (Exception ex)
            {
                return GenericResponse<IEnumerable<SubscriptionResponseDto>>.CreateError($"Error retrieving all subscriptions: {ex.Message}");
            }
        }

        public async Task<GenericResponse<IEnumerable<PaymentResponseDto>>> GetUserPaymentsByAdminAsync(Guid targetUserId)
        {
            try
            {
                var payments = await _paymentRepository.GetUserPaymentHistoryAsync(targetUserId);

                var responses = payments.Select(payment => new PaymentResponseDto
                {
                    Id = payment.Id,
                    UserId = payment.UserId,
                    UserEmail = payment.User?.Email,
                    SubscriptionId = payment.SubscriptionId,
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    Status = payment.Status,
                    PaymentMethod = payment.PaymentMethod,
                    TransactionId = payment.TransactionId,
                    InvoiceUrl = payment.InvoiceUrl,
                    CreatedAt = payment.CreatedAt
                });

                return GenericResponse<IEnumerable<PaymentResponseDto>>.CreateSuccess(responses, $"Payments for user {targetUserId} retrieved successfully");
            }
            catch (Exception ex)
            {
                return GenericResponse<IEnumerable<PaymentResponseDto>>.CreateError($"Error retrieving user payments: {ex.Message}");
            }
        }

        public async Task<GenericResponse<IEnumerable<SubscriptionResponseDto>>> GetUserSubscriptionsByAdminAsync(Guid targetUserId)
        {
            try
            {
                var subscriptions = await _subscriptionRepository.GetByUserIdAsync(targetUserId);

                var responses = subscriptions.Select(subscription => new SubscriptionResponseDto
                {
                    Id = subscription.Id,
                    ProfileId = subscription.ProfileId,
                    Plan = subscription.Plan,
                    QuotaPostsPerMonth = subscription.QuotaPostsPerMonth,
                    QuotaStorageGb = subscription.QuotaStorageGb,
                    StartDate = subscription.StartDate,
                    EndDate = subscription.EndDate,
                    IsActive = subscription.IsActive,
                    CreatedAt = subscription.CreatedAt,
                    StripeSubscriptionId = subscription.StripeSubscriptionId,
                    StripeCustomerId = subscription.StripeCustomerId
                });

                return GenericResponse<IEnumerable<SubscriptionResponseDto>>.CreateSuccess(responses, $"Subscriptions for user {targetUserId} retrieved successfully");
            }
            catch (Exception ex)
            {
                return GenericResponse<IEnumerable<SubscriptionResponseDto>>.CreateError($"Error retrieving user subscriptions: {ex.Message}");
            }
        }
    }
}