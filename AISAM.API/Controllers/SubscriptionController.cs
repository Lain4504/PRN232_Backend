using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AISAM.Common;
using AISAM.Services.IServices;
using AISAM.Common.Dtos;
using AISAM.API.Utils;
using AISAM.API.Validators;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/subscriptions")]
    [Authorize]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILogger<SubscriptionController> _logger;

        public SubscriptionController(ISubscriptionService subscriptionService, ILogger<SubscriptionController> logger)
        {
            _subscriptionService = subscriptionService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<GenericResponse<SubscriptionDto>>> CreateSubscription([FromBody] CreateSubscriptionRequest request)
        {
            var validator = new CreateSubscriptionRequestValidator();
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(GenericResponse<SubscriptionDto>.CreateError(
                    string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))));
            }

            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);

                // Check permissions for subscription management
                if (!await _subscriptionService.CheckSubscriptionPermissionsAsync(userId))
                {
                    return StatusCode(403, GenericResponse<SubscriptionDto>.CreateError("Insufficient permissions"));
                }

                var subscription = await _subscriptionService.CreateSubscriptionAsync(userId, request.Plan);

                return Ok(GenericResponse<SubscriptionDto>.CreateSuccess(subscription, "Subscription created successfully"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(GenericResponse<SubscriptionDto>.CreateError("Unauthorized"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(GenericResponse<SubscriptionDto>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription");
                return StatusCode(500, GenericResponse<SubscriptionDto>.CreateError("Internal server error"));
            }
        }

        [HttpGet]
        public async Task<ActionResult<GenericResponse<List<SubscriptionDto>>>> GetSubscriptions()
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);

                var subscriptions = await _subscriptionService.GetUserSubscriptionsAsync(userId);

                return Ok(GenericResponse<List<SubscriptionDto>>.CreateSuccess(subscriptions, "Subscriptions retrieved successfully"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(GenericResponse<List<SubscriptionDto>>.CreateError("Unauthorized"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscriptions");
                return StatusCode(500, GenericResponse<List<SubscriptionDto>>.CreateError("Internal server error"));
            }
        }

        [HttpGet("active")]
        public async Task<ActionResult<GenericResponse<SubscriptionDto>>> GetActiveSubscription()
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);

                var subscription = await _subscriptionService.GetActiveSubscriptionAsync(userId);

                if (subscription == null)
                {
                    return NotFound(GenericResponse<SubscriptionDto>.CreateError("No active subscription found"));
                }

                return Ok(GenericResponse<SubscriptionDto>.CreateSuccess(subscription, "Active subscription retrieved successfully"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(GenericResponse<SubscriptionDto>.CreateError("Unauthorized"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active subscription");
                return StatusCode(500, GenericResponse<SubscriptionDto>.CreateError("Internal server error"));
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<GenericResponse<SubscriptionDto>>> UpdateSubscription(Guid id, [FromBody] UpdateSubscriptionRequest request)
        {
            var validator = new UpdateSubscriptionRequestValidator();
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(GenericResponse<SubscriptionDto>.CreateError(
                    string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))));
            }

            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);

                var subscription = await _subscriptionService.UpdateSubscriptionAsync(userId, id, request.Plan);

                return Ok(GenericResponse<SubscriptionDto>.CreateSuccess(subscription, "Subscription updated successfully"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(GenericResponse<SubscriptionDto>.CreateError("Unauthorized"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(GenericResponse<SubscriptionDto>.CreateError("Subscription not found"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(GenericResponse<SubscriptionDto>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription");
                return StatusCode(500, GenericResponse<SubscriptionDto>.CreateError("Internal server error"));
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<GenericResponse<string>>> CancelSubscription(Guid id)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);

                await _subscriptionService.CancelSubscriptionAsync(userId, id);

                return Ok(GenericResponse<string>.CreateSuccess("Subscription canceled successfully"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(GenericResponse<string>.CreateError("Unauthorized"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(GenericResponse<string>.CreateError("Subscription not found"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(GenericResponse<string>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling subscription");
                return StatusCode(500, GenericResponse<string>.CreateError("Internal server error"));
            }
        }

        [HttpPost("check-quota")]
        public async Task<ActionResult<GenericResponse<bool>>> CheckQuota([FromQuery] string quotaType, [FromQuery] int amount = 1)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);

                var hasQuota = await _subscriptionService.CheckQuotaAsync(userId, quotaType, amount);

                return Ok(GenericResponse<bool>.CreateSuccess(hasQuota, hasQuota ? "Quota available" : "Quota exceeded"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(GenericResponse<bool>.CreateError("Unauthorized"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking quota");
                return StatusCode(500, GenericResponse<bool>.CreateError("Internal server error"));
            }
        }
    }
}