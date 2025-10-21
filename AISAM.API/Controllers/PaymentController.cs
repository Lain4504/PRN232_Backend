using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AISAM.Common;
using AISAM.Services.IServices;
using AISAM.Common.Dtos;
using AISAM.API.Utils;
using AISAM.API.Validators;
using AISAM.Data.Enumeration;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/payments")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        [HttpPost("create-order")]
        public async Task<ActionResult<GenericResponse<PaymentOrderDto>>> CreateOrder([FromQuery] SubscriptionPlanEnum plan)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);

                var order = await _paymentService.CreateOrderAsync(userId, plan);

                return Ok(GenericResponse<PaymentOrderDto>.CreateSuccess(order, "Payment order created successfully"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(GenericResponse<PaymentOrderDto>.CreateError("Unauthorized"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment order");
                return StatusCode(500, GenericResponse<PaymentOrderDto>.CreateError("Internal server error"));
            }
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> HandleWebhook([FromBody] PaymentWebhookRequest request)
        {
            var validator = new PaymentWebhookRequestValidator();
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
            }

            try
            {
                bool success;
                if (request.Status.ToLower() == "success")
                {
                    success = await _paymentService.HandlePaymentSuccessAsync(request.OrderId);
                }
                else if (request.Status.ToLower() == "failed")
                {
                    success = await _paymentService.HandlePaymentFailureAsync(request.OrderId);
                }
                else
                {
                    _logger.LogWarning("Invalid payment status: {Status}", request.Status);
                    return BadRequest("Invalid status");
                }

                if (!success)
                {
                    return NotFound("Payment order not found");
                }

                return Ok(new { message = "Webhook processed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment webhook");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}