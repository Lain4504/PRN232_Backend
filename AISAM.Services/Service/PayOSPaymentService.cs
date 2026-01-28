using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Common;
using AISAM.Data.Model;
using AISAM.Data.Enumeration;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AISAM.Services.Service
{
    public class PayOSPaymentService : IPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly IPaymentRepository _paymentRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IProfileRepository _profileRepository;
        private readonly HttpClient _httpClient;

        private readonly string _clientId;
        private readonly string _apiKey;
        private readonly string _checksumKey;
        private readonly string _baseUrl = "https://api-merchant.payos.vn";

        public PayOSPaymentService(
            IConfiguration configuration,
            IPaymentRepository paymentRepository,
            ISubscriptionRepository subscriptionRepository,
            IUserRepository userRepository,
            IProfileRepository profileRepository,
            HttpClient httpClient)
        {
            _configuration = configuration;
            _paymentRepository = paymentRepository;
            _subscriptionRepository = subscriptionRepository;
            _userRepository = userRepository;
            _profileRepository = profileRepository;
            _httpClient = httpClient;

            _clientId = Environment.GetEnvironmentVariable("PAYOS_CLIENT_ID") ?? _configuration["PayOS:ClientId"] ?? "";
            _apiKey = Environment.GetEnvironmentVariable("PAYOS_API_KEY") ?? _configuration["PayOS:ApiKey"] ?? "";
            _checksumKey = Environment.GetEnvironmentVariable("PAYOS_CHECKSUM_KEY") ?? _configuration["PayOS:ChecksumKey"] ?? "";
        }

        public async Task<GenericResponse<PayOSCheckoutResponse>> CreatePaymentLinkAsync(CreatePaymentIntentRequest request, Guid userId, Guid profileId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null) return GenericResponse<PayOSCheckoutResponse>.CreateError("User not found");

                var plan = (SubscriptionPlanEnum)request.SubscriptionPlanId;
                var amount = GetPlanPrice(plan);
                var orderCode = long.Parse(DateTime.UtcNow.ToString("yyMMddHHmmssfff")); // Generate a unique order code

                var data = new
                {
                    orderCode = orderCode,
                    amount = (int)amount,
                    description = $"Thanh toan goi {plan}",
                    cancelUrl = $"{_configuration["FrontendSettings:BaseUrl"]}/subscription/cancel",
                    returnUrl = $"{_configuration["FrontendSettings:BaseUrl"]}/subscription/success?orderCode={orderCode}",
                };

                // Create Pending Subscription Record
                var planDetails = GetPlanDetails(plan);
                var subscription = new Data.Model.Subscription
                {
                    Id = Guid.NewGuid(),
                    ProfileId = profileId,
                    Plan = plan,
                    QuotaPostsPerMonth = planDetails.quotaPosts,
                    QuotaAIContentPerDay = planDetails.quotaAIContent,
                    QuotaAIImagesPerDay = planDetails.quotaAIImages,
                    QuotaPlatforms = planDetails.quotaPlatforms,
                    QuotaAccounts = planDetails.quotaAccounts,
                    AnalysisLevel = planDetails.analysisLevel,
                    StartDate = DateTime.UtcNow.Date,
                    EndDate = DateTime.UtcNow.Date.AddMonths(1),
                    IsActive = false, // Pending payment
                    PayOSOrderCode = orderCode.ToString(),
                    CreatedAt = DateTime.UtcNow
                };

                await _subscriptionRepository.CreateAsync(subscription);

                // Create signature
                var signatureData = $"amount={data.amount}&cancelUrl={data.cancelUrl}&description={data.description}&orderCode={data.orderCode}&returnUrl={data.returnUrl}";
                var signature = CreateSignature(signatureData, _checksumKey);

                var payload = new
                {
                    orderCode = data.orderCode,
                    amount = data.amount,
                    description = data.description,
                    cancelUrl = data.cancelUrl,
                    returnUrl = data.returnUrl,
                    signature = signature
                };

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-client-id", _clientId);
                _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);

                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/v2/payment-requests", payload);
                var resultJson = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    return GenericResponse<PayOSCheckoutResponse>.CreateError($"PayOS Error: {resultJson}");
                }

                using var doc = JsonDocument.Parse(resultJson);
                var root = doc.RootElement;
                if (root.GetProperty("code").GetString() != "00")
                {
                    return GenericResponse<PayOSCheckoutResponse>.CreateError($"PayOS Error: {root.GetProperty("desc").GetString()}");
                }

                var payosData = root.GetProperty("data");
                
                return GenericResponse<PayOSCheckoutResponse>.CreateSuccess(new PayOSCheckoutResponse
                {
                    CheckoutUrl = payosData.GetProperty("checkoutUrl").GetString() ?? "",
                    PaymentLinkId = payosData.GetProperty("paymentLinkId").GetString() ?? "",
                    OrderCode = orderCode,
                    Amount = amount,
                    Status = "PENDING"
                }, "Payment link created successfully");
            }
            catch (Exception ex)
            {
                return GenericResponse<PayOSCheckoutResponse>.CreateError($"Error creating payment link: {ex.Message}");
            }
        }

        public async Task<GenericResponse<PaymentResponseDto>> ConfirmPaymentAsync(long orderCode, Guid userId)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-client-id", _clientId);
                _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);

                var response = await _httpClient.GetAsync($"{_baseUrl}/v2/payment-requests/{orderCode}");
                var resultJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return GenericResponse<PaymentResponseDto>.CreateError($"PayOS Error: {resultJson}");
                }

                using var doc = JsonDocument.Parse(resultJson);
                var root = doc.RootElement;
                var payosData = root.GetProperty("data");
                var status = payosData.GetProperty("status").GetString();

                if (status == "PAID")
                {
                    // Update or create payment record
                    var payment = await _paymentRepository.GetByTransactionIdAsync(orderCode.ToString());
                    if (payment == null)
                    {
                        payment = new Payment
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            Amount = payosData.GetProperty("amount").GetDecimal(),
                            Currency = "VND",
                            Status = PaymentStatusEnum.Success,
                            TransactionId = orderCode.ToString(),
                            PaymentMethod = "payos",
                            CreatedAt = DateTime.UtcNow
                        };
                        await _paymentRepository.CreateAsync(payment);
                    }
                    else if (payment.Status != PaymentStatusEnum.Success)
                    {
                        payment.Status = PaymentStatusEnum.Success;
                        await _paymentRepository.UpdateAsync(payment);
                    }

                    // Activate subscription
                    var subscription = await _subscriptionRepository.GetByPayOSOrderCodeAsync(orderCode.ToString());
                    if (subscription != null && !subscription.IsActive)
                    {
                        subscription.IsActive = true;
                        subscription.StartDate = DateTime.UtcNow.Date;
                        subscription.EndDate = DateTime.UtcNow.Date.AddMonths(1);
                        await _subscriptionRepository.UpdateAsync(subscription);

                        // Update Profile
                        var profile = await _profileRepository.GetByIdAsync(subscription.ProfileId);
                        if (profile != null)
                        {
                            profile.Status = ProfileStatusEnum.Active;
                            profile.SubscriptionId = subscription.Id;
                            profile.ProfileType = subscription.Plan == SubscriptionPlanEnum.Plus ? ProfileTypeEnum.Basic : 
                                                 (subscription.Plan == SubscriptionPlanEnum.Premium ? ProfileTypeEnum.Pro : ProfileTypeEnum.Free);
                            await _profileRepository.UpdateAsync(profile);
                            await _profileRepository.UpdateAsync(profile);
                        }
                    }

                    return GenericResponse<PaymentResponseDto>.CreateSuccess(new PaymentResponseDto
                    {
                        Id = payment.Id,
                        UserId = userId,
                        Amount = payment.Amount,
                        Currency = payment.Currency,
                        Status = payment.Status,
                        TransactionId = payment.TransactionId,
                        PaymentMethod = payment.PaymentMethod,
                        CreatedAt = payment.CreatedAt
                    }, "Payment confirmed successfully");
                }

                return GenericResponse<PaymentResponseDto>.CreateError($"Payment status: {status}");
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
                var profile = await _profileRepository.GetByIdAsync(request.ProfileId);
                if (profile == null || profile.UserId != userId)
                {
                    return GenericResponse<SubscriptionResponseDto>.CreateError("Profile not found or access denied");
                }

                // Handle Free plan activation
                if (request.Plan == SubscriptionPlanEnum.Free)
                {
                    var details = GetPlanDetails(SubscriptionPlanEnum.Free);
                    var subscription = new Data.Model.Subscription
                    {
                        Id = Guid.NewGuid(),
                        ProfileId = request.ProfileId,
                        Plan = SubscriptionPlanEnum.Free,
                        QuotaPostsPerMonth = details.quotaPosts,
                        QuotaAIContentPerDay = details.quotaAIContent,
                        QuotaAIImagesPerDay = details.quotaAIImages,
                        QuotaPlatforms = details.quotaPlatforms,
                        QuotaAccounts = details.quotaAccounts,
                        AnalysisLevel = details.analysisLevel,
                        StartDate = DateTime.UtcNow.Date,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var created = await _subscriptionRepository.CreateAsync(subscription);
                    
                    profile.Status = ProfileStatusEnum.Active;
                    profile.SubscriptionId = created.Id;
                    profile.ProfileType = ProfileTypeEnum.Free;
                    await _profileRepository.UpdateAsync(profile);

                    return GenericResponse<SubscriptionResponseDto>.CreateSuccess(MapToDto(created), "Free subscription activated");
                }

                // For paid plans, we assume payment is handled via CreatePaymentLinkAsync and webhook/confirm
                // This method might be called after payment confirmation if not handled automatically
                return GenericResponse<SubscriptionResponseDto>.CreateError("Use payment flow for paid plans");
            }
            catch (Exception ex)
            {
                return GenericResponse<SubscriptionResponseDto>.CreateError($"Error creating subscription: {ex.Message}");
            }
        }

        public async Task<GenericResponse<bool>> CancelSubscriptionAsync(Guid subscriptionId, Guid userId)
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
            if (subscription == null) return GenericResponse<bool>.CreateError("Subscription not found");

            var profile = await _profileRepository.GetByIdAsync(subscription.ProfileId);
            if (profile == null || profile.UserId != userId) return GenericResponse<bool>.CreateError("Access denied");

            subscription.IsActive = false;
            subscription.EndDate = DateTime.UtcNow.Date;
            await _subscriptionRepository.UpdateAsync(subscription);

            return GenericResponse<bool>.CreateSuccess(true, "Subscription cancelled");
        }

        public async Task<GenericResponse<SubscriptionResponseDto>> GetSubscriptionAsync(Guid subscriptionId, Guid userId)
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
            if (subscription == null) return GenericResponse<SubscriptionResponseDto>.CreateError("Subscription not found");

            var profile = await _profileRepository.GetByIdAsync(subscription.ProfileId);
            if (profile == null || profile.UserId != userId) return GenericResponse<SubscriptionResponseDto>.CreateError("Access denied");

            return GenericResponse<SubscriptionResponseDto>.CreateSuccess(MapToDto(subscription));
        }

        public async Task<GenericResponse<SubscriptionResponseDto>> GetActiveSubscriptionByProfileIdAsync(Guid profileId, Guid userId)
        {
            var profile = await _profileRepository.GetByIdAsync(profileId);
            if (profile == null || profile.UserId != userId) return GenericResponse<SubscriptionResponseDto>.CreateError("Access denied");

            var subscription = await _subscriptionRepository.GetActiveByProfileIdAsync(profileId);
            if (subscription == null) return GenericResponse<SubscriptionResponseDto>.CreateError("No active subscription");

            return GenericResponse<SubscriptionResponseDto>.CreateSuccess(MapToDto(subscription));
        }

        public async Task<GenericResponse<IEnumerable<SubscriptionResponseDto>>> GetUserSubscriptionsAsync(Guid userId)
        {
            var subs = await _subscriptionRepository.GetByUserIdAsync(userId);
            return GenericResponse<IEnumerable<SubscriptionResponseDto>>.CreateSuccess(subs.Select(MapToDto));
        }

        public async Task<GenericResponse<IEnumerable<PaymentResponseDto>>> GetUserPaymentHistoryAsync(Guid userId)
        {
            var payments = await _paymentRepository.GetUserPaymentHistoryAsync(userId);
            return GenericResponse<IEnumerable<PaymentResponseDto>>.CreateSuccess(payments.Select(p => new PaymentResponseDto
            {
                Id = p.Id,
                UserId = p.UserId,
                Amount = p.Amount,
                Currency = p.Currency,
                Status = p.Status,
                PaymentMethod = p.PaymentMethod,
                TransactionId = p.TransactionId,
                CreatedAt = p.CreatedAt
            }));
        }

        public async Task<GenericResponse<SubscriptionResponseDto>> ChangePlanAsync(ChangePlanRequest request, Guid userId, Guid profileId)
        {
            // Plan change logic simplified for now, usually requires new payment for upgrade
            return GenericResponse<SubscriptionResponseDto>.CreateError("Plan changing requires new payment checkout");
        }

        public async Task<GenericResponse<bool>> HandleWebhookAsync(string json, string signature)
        {
            try
            {
                // Verify signature
                // PayOS webhook signature verification logic here
                // For now, assume it's valid if we are at this step
                
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var data = root.GetProperty("data");
                var orderCode = data.GetProperty("orderCode").GetInt64().ToString();
                var status = root.GetProperty("code").GetString(); // Simplified

                if (status == "00") // Success
                {
                    var subscription = await _subscriptionRepository.GetByPayOSOrderCodeAsync(orderCode);
                    if (subscription != null && !subscription.IsActive)
                    {
                        subscription.IsActive = true;
                        subscription.StartDate = DateTime.UtcNow.Date;
                        subscription.EndDate = DateTime.UtcNow.Date.AddMonths(1);
                        await _subscriptionRepository.UpdateAsync(subscription);

                        // Update Profile
                        var profile = await _profileRepository.GetByIdAsync(subscription.ProfileId);
                        if (profile != null)
                        {
                            profile.Status = ProfileStatusEnum.Active;
                            profile.SubscriptionId = subscription.Id;
                            profile.ProfileType = subscription.Plan == SubscriptionPlanEnum.Plus ? ProfileTypeEnum.Basic :
                                                 (subscription.Plan == SubscriptionPlanEnum.Premium ? ProfileTypeEnum.Pro : ProfileTypeEnum.Free);
                            await _profileRepository.UpdateAsync(profile);
                        }
                        
                        // Create payment record if not exists (similar logic to ConfirmPaymentAsync)
                        // Create payment record if not exists (critical for audit when ConfirmPayment is missed)
                        var payment = await _paymentRepository.GetByTransactionIdAsync(orderCode);
                        if (payment == null)
                        {
                            try 
                            {
                                // Parse amount from webhook data
                                var amount = data.GetProperty("amount").GetDecimal();
                                var userIdFromSub = (await _profileRepository.GetByIdAsync(subscription.ProfileId))?.UserId;
                                
                                if (userIdFromSub.HasValue)
                                {
                                    payment = new Payment
                                    {
                                        Id = Guid.NewGuid(),
                                        UserId = userIdFromSub.Value,
                                        Amount = amount,
                                        Currency = "VND",
                                        Status = PaymentStatusEnum.Success,
                                        TransactionId = orderCode,
                                        PaymentMethod = "payos",
                                        CreatedAt = DateTime.UtcNow
                                    };
                                    await _paymentRepository.CreateAsync(payment);
                                }
                            }
                            catch (Exception ex)
                            {
                                // Log error but don't fail the webhook processing since subscription is active
                                Console.WriteLine($"Error creating payment record in webhook: {ex.Message}");
                            }
                        }
                    }
                }

                return GenericResponse<bool>.CreateSuccess(true);
            }
            catch (Exception)
            {
                return GenericResponse<bool>.CreateError("Webhook processing failed");
            }
        }

        public async Task<GenericResponse<IEnumerable<PaymentResponseDto>>> GetAllPaymentsAsync()
        {
            var payments = await _paymentRepository.GetAllPaymentsAsync();
            return GenericResponse<IEnumerable<PaymentResponseDto>>.CreateSuccess(payments.Select(p => new PaymentResponseDto
            {
                Id = p.Id,
                UserId = p.UserId,
                UserEmail = p.User?.Email,
                SubscriptionId = p.SubscriptionId,
                Amount = p.Amount,
                Currency = p.Currency,
                Status = p.Status,
                PaymentMethod = p.PaymentMethod,
                TransactionId = p.TransactionId,
                InvoiceUrl = p.InvoiceUrl,
                CreatedAt = p.CreatedAt
            }));
        }

        public async Task<GenericResponse<IEnumerable<SubscriptionResponseDto>>> GetAllSubscriptionsAsync()
        {
            var subs = await _subscriptionRepository.GetAllSubscriptionsAsync();
            return GenericResponse<IEnumerable<SubscriptionResponseDto>>.CreateSuccess(subs.Select(MapToDto));
        }

        public async Task<GenericResponse<IEnumerable<PaymentResponseDto>>> GetUserPaymentsByAdminAsync(Guid targetUserId)
        {
            var payments = await _paymentRepository.GetUserPaymentHistoryAsync(targetUserId);
            return GenericResponse<IEnumerable<PaymentResponseDto>>.CreateSuccess(payments.Select(p => new PaymentResponseDto
            {
                Id = p.Id,
                UserId = p.UserId,
                UserEmail = p.User?.Email,
                SubscriptionId = p.SubscriptionId,
                Amount = p.Amount,
                Currency = p.Currency,
                Status = p.Status,
                PaymentMethod = p.PaymentMethod,
                TransactionId = p.TransactionId,
                InvoiceUrl = p.InvoiceUrl,
                CreatedAt = p.CreatedAt
            }));
        }

        public async Task<GenericResponse<IEnumerable<SubscriptionResponseDto>>> GetUserSubscriptionsByAdminAsync(Guid targetUserId)
        {
            var subs = await _subscriptionRepository.GetByUserIdAsync(targetUserId);
            return GenericResponse<IEnumerable<SubscriptionResponseDto>>.CreateSuccess(subs.Select(MapToDto));
        }

        private (int quotaPosts, int quotaAIContent, int quotaAIImages, int quotaPlatforms, int quotaAccounts, int analysisLevel) GetPlanDetails(SubscriptionPlanEnum plan)
        {
            return plan switch
            {
                SubscriptionPlanEnum.Free => (5, 0, 0, 1, 1, 0),
                SubscriptionPlanEnum.Plus => (30, 2, 7, 2, 3, 1),
                SubscriptionPlanEnum.Premium => (-1, 4, 10, 3, 5, 2),
                _ => (5, 0, 0, 1, 1, 0)
            };
        }

        private decimal GetPlanPrice(SubscriptionPlanEnum plan)
        {
            return plan switch
            {
                SubscriptionPlanEnum.Free => 0,
                SubscriptionPlanEnum.Plus => 359000,
                SubscriptionPlanEnum.Premium => 559000,
                _ => 0
            };
        }

        private SubscriptionResponseDto MapToDto(Data.Model.Subscription sub)
        {
            return new SubscriptionResponseDto
            {
                Id = sub.Id,
                ProfileId = sub.ProfileId,
                Plan = sub.Plan,
                QuotaPostsPerMonth = sub.QuotaPostsPerMonth,
                QuotaAIContentPerDay = sub.QuotaAIContentPerDay,
                QuotaAIImagesPerDay = sub.QuotaAIImagesPerDay,
                QuotaPlatforms = sub.QuotaPlatforms,
                QuotaAccounts = sub.QuotaAccounts,
                AnalysisLevel = sub.AnalysisLevel,
                QuotaAdBudgetMonthly = sub.QuotaAdBudgetMonthly,
                QuotaAdCampaigns = sub.QuotaAdCampaigns,
                StartDate = sub.StartDate,
                EndDate = sub.EndDate,
                IsActive = sub.IsActive,
                CreatedAt = sub.CreatedAt,
                PayOSOrderCode = sub.PayOSOrderCode,
                PayOSPaymentLinkId = sub.PayOSPaymentLinkId
            };
        }

        private string CreateSignature(string data, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(dataBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}
