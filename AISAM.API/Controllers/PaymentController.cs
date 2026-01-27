using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Common;
using AISAM.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AISAM.API.Utils;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/payment")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        /// <summary>
        /// Create a PayOS checkout link for subscription payment
        /// </summary>
        [HttpPost("create-checkout-link")]
        public async Task<ActionResult<GenericResponse<PayOSCheckoutResponse>>> CreateCheckoutLink([FromBody] CreatePaymentIntentRequest request)
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            if (userId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<PayOSCheckoutResponse>.CreateError("Không thể xác thực người dùng"));
            }

            var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);

            var result = await _paymentService.CreatePaymentLinkAsync(request, userId, profileId);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Confirm a payment after successful checkout
        /// </summary>
        [HttpPost("confirm/{orderCode}")]
        public async Task<ActionResult<GenericResponse<PaymentResponseDto>>> ConfirmPayment(long orderCode)
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            if (userId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<PaymentResponseDto>.CreateError("Không thể xác thực người dùng"));
            }

            var result = await _paymentService.ConfirmPaymentAsync(orderCode, userId);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Create a new subscription
        /// </summary>
        [HttpPost("subscription")]
        public async Task<ActionResult<GenericResponse<SubscriptionResponseDto>>> CreateSubscription([FromBody] CreateSubscriptionRequest request)
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            if (userId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<SubscriptionResponseDto>.CreateError("Không thể xác thực người dùng"));
            }

            var result = await _paymentService.CreateSubscriptionAsync(request, userId);

            if (result.Success)
            {
                return CreatedAtAction(nameof(GetSubscription), new { id = result.Data!.Id }, result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Get subscription by ID
        /// </summary>
        [HttpGet("subscription/{id}")]
        public async Task<ActionResult<GenericResponse<SubscriptionResponseDto>>> GetSubscription(Guid id)
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            if (userId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<SubscriptionResponseDto>.CreateError("Không thể xác thực người dùng"));
            }

            var result = await _paymentService.GetSubscriptionAsync(id, userId);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Get active subscription for current profile
        /// </summary>
        [HttpGet("subscription/active")]
        public async Task<ActionResult<GenericResponse<SubscriptionResponseDto>>> GetActiveSubscription()
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            if (userId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<SubscriptionResponseDto>.CreateError("Không thể xác thực người dùng"));
            }

            var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);

            var result = await _paymentService.GetActiveSubscriptionByProfileIdAsync(profileId, userId);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Get user's subscriptions
        /// </summary>
        [HttpGet("subscriptions")]
        public async Task<ActionResult<GenericResponse<IEnumerable<SubscriptionResponseDto>>>> GetUserSubscriptions()
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            if (userId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<IEnumerable<SubscriptionResponseDto>>.CreateError("Không thể xác thực người dùng"));
            }

            var result = await _paymentService.GetUserSubscriptionsAsync(userId);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Cancel subscription
        /// </summary>
        [HttpDelete("subscription/{id}")]
        public async Task<ActionResult<GenericResponse<bool>>> CancelSubscription(Guid id)
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            if (userId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<bool>.CreateError("Không thể xác thực người dùng"));
            }

            var result = await _paymentService.CancelSubscriptionAsync(id, userId);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Change subscription plan
        /// </summary>
        [HttpPut("subscription/change-plan")]
        public async Task<ActionResult<GenericResponse<SubscriptionResponseDto>>> ChangePlan([FromBody] ChangePlanRequest request)
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            if (userId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<SubscriptionResponseDto>.CreateError("Không thể xác thực người dùng"));
            }

            var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);

            var result = await _paymentService.ChangePlanAsync(request, userId, profileId);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Get user's payment history
        /// </summary>
        [HttpGet("history")]
        public async Task<ActionResult<GenericResponse<IEnumerable<PaymentResponseDto>>>> GetUserPaymentHistory()
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            if (userId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<IEnumerable<PaymentResponseDto>>.CreateError("Không thể xác thực người dùng"));
            }

            var result = await _paymentService.GetUserPaymentHistoryAsync(userId);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Handle PayOS webhooks
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleWebhook()
        {
            try
            {
                var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                var signature = Request.Headers["x-api-validate-signature"].ToString();

                var result = await _paymentService.HandleWebhookAsync(json, signature);

                if (result.Success)
                {
                    return Ok();
                }

                return BadRequest();
            }
            catch (Exception ex)
            {
                return BadRequest($"Webhook error: {ex.Message}");
            }
        }

        // Admin endpoints
        /// <summary>
        /// Get all payments (Admin only)
        /// </summary>
        [HttpGet("admin/all")]
        public async Task<ActionResult<GenericResponse<IEnumerable<PaymentResponseDto>>>> GetAllPayments([FromServices] IUserService userService)
        {
            try
            {
                await UserClaimsHelper.EnsureAdminAsync(User, userService);

                var result = await _paymentService.GetAllPaymentsAsync();

                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(GenericResponse<IEnumerable<PaymentResponseDto>>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, GenericResponse<IEnumerable<PaymentResponseDto>>.CreateError($"Error retrieving all payments: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get all subscriptions (Admin only)
        /// </summary>
        [HttpGet("admin/subscriptions")]
        public async Task<ActionResult<GenericResponse<IEnumerable<SubscriptionResponseDto>>>> GetAllSubscriptions([FromServices] IUserService userService)
        {
            try
            {
                await UserClaimsHelper.EnsureAdminAsync(User, userService);

                var result = await _paymentService.GetAllSubscriptionsAsync();

                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(GenericResponse<IEnumerable<SubscriptionResponseDto>>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, GenericResponse<IEnumerable<SubscriptionResponseDto>>.CreateError($"Error retrieving all subscriptions: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get user payments by admin (Admin only)
        /// </summary>
        [HttpGet("admin/user/{userId}/payments")]
        public async Task<ActionResult<GenericResponse<IEnumerable<PaymentResponseDto>>>> GetUserPaymentsByAdmin(
            Guid userId,
            [FromServices] IUserService userService)
        {
            try
            {
                await UserClaimsHelper.EnsureAdminAsync(User, userService);

                var result = await _paymentService.GetUserPaymentsByAdminAsync(userId);

                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(GenericResponse<IEnumerable<PaymentResponseDto>>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, GenericResponse<IEnumerable<PaymentResponseDto>>.CreateError($"Error retrieving user payments: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get user subscriptions by admin (Admin only)
        /// </summary>
        [HttpGet("admin/user/{userId}/subscriptions")]
        public async Task<ActionResult<GenericResponse<IEnumerable<SubscriptionResponseDto>>>> GetUserSubscriptionsByAdmin(
            Guid userId,
            [FromServices] IUserService userService)
        {
            try
            {
                await UserClaimsHelper.EnsureAdminAsync(User, userService);

                var result = await _paymentService.GetUserSubscriptionsByAdminAsync(userId);

                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(GenericResponse<IEnumerable<SubscriptionResponseDto>>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, GenericResponse<IEnumerable<SubscriptionResponseDto>>.CreateError($"Error retrieving user subscriptions: {ex.Message}"));
            }
        }
    }
}