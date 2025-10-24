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
        /// Create a payment intent for subscription payment
        /// </summary>
        [HttpPost("create-payment-intent")]
        public async Task<ActionResult<GenericResponse<CreatePaymentIntentResponse>>> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            if (userId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<CreatePaymentIntentResponse>.CreateError("Không thể xác thực người dùng"));
            }

            var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);

            var result = await _paymentService.CreatePaymentIntentAsync(request, userId, profileId);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        /// <summary>
        /// Confirm a payment after successful payment intent
        /// </summary>
        [HttpPost("confirm/{paymentIntentId}")]
        public async Task<ActionResult<GenericResponse<PaymentResponseDto>>> ConfirmPayment(string paymentIntentId)
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            if (userId == Guid.Empty)
            {
                return Unauthorized(GenericResponse<PaymentResponseDto>.CreateError("Không thể xác thực người dùng"));
            }

            var result = await _paymentService.ConfirmPaymentAsync(paymentIntentId, userId);

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
        /// Handle Stripe webhooks
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleWebhook()
        {
            try
            {
                var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                var stripeSignature = Request.Headers["Stripe-Signature"].ToString();

                var result = await _paymentService.HandleWebhookAsync(json, stripeSignature);

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
    }
}