using AISAM.API.Utils;
using AISAM.Common;
using AISAM.Common.Dtos.Response;
using AISAM.Data.Enumeration;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/content-calendar")]
    public class ContentCalendarController : ControllerBase
    {
        private readonly IScheduledPostingService _scheduledPostingService;
        private readonly IContentService _contentService;
        private readonly ISocialIntegrationRepository _socialIntegrationRepository;

        public ContentCalendarController(
            IScheduledPostingService scheduledPostingService,
            IContentService contentService,
            ISocialIntegrationRepository socialIntegrationRepository)
        {
            _scheduledPostingService = scheduledPostingService;
            _contentService = contentService;
            _socialIntegrationRepository = socialIntegrationRepository;
        }

        /// <summary>
        /// Schedule content for posting at a specific date and time
        /// </summary>
        [HttpPost("schedule/{contentId}")]
        public async Task<IActionResult> ScheduleContent(
            Guid contentId,
            [FromBody] ScheduleContentRequest request)
        {
            try
            {
                var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);
                var content = await _contentService.GetContentByIdAsync(contentId, profileId);

                if (content == null)
                {
                    return NotFound(GenericResponse<object>.CreateError("Content not found", System.Net.HttpStatusCode.NotFound, "NOT_FOUND"));
                }

                // Validate integration IDs
                if (request.IntegrationIds != null && request.IntegrationIds.Any())
                {
                    var userIntegrations = await _socialIntegrationRepository.GetByProfileIdAsync(profileId);
                    var validIntegrationIds = userIntegrations.Select(i => i.Id).ToList();

                    var invalidIds = request.IntegrationIds.Where(id => !validIntegrationIds.Contains(id)).ToList();
                    if (invalidIds.Any())
                    {
                        return BadRequest(GenericResponse<object>.CreateError($"Invalid integration IDs: {string.Join(", ", invalidIds)}", System.Net.HttpStatusCode.BadRequest, "INVALID_INTEGRATIONS"));
                    }
                }

                var result = await _scheduledPostingService.ScheduleContentAsync(
                    contentId,
                    request.ScheduledDate,
                    request.ScheduledTime,
                    request.Timezone,
                    request.IntegrationIds);

                return Ok(GenericResponse<ContentResponseDto>.CreateSuccess(result, "Content scheduled successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(GenericResponse<object>.CreateError(ex.Message, System.Net.HttpStatusCode.BadRequest, "SCHEDULE_ERROR"));
            }
        }

        /// <summary>
        /// Schedule recurring content posting
        /// </summary>
        [HttpPost("schedule-recurring/{contentId}")]
        public async Task<IActionResult> ScheduleRecurringContent(
            Guid contentId,
            [FromBody] ScheduleRecurringContentRequest request)
        {
            try
            {
                var profileId = ProfileContextHelper.GetActiveProfileIdOrThrow(HttpContext);
                var content = await _contentService.GetContentByIdAsync(contentId, profileId);

                if (content == null)
                {
                    return NotFound(GenericResponse<object>.CreateError("Content not found", System.Net.HttpStatusCode.NotFound, "NOT_FOUND"));
                }

                // Validate integration IDs
                if (request.IntegrationIds != null && request.IntegrationIds.Any())
                {
                    var userIntegrations = await _socialIntegrationRepository.GetByProfileIdAsync(profileId);
                    var validIntegrationIds = userIntegrations.Select(i => i.Id).ToList();

                    var invalidIds = request.IntegrationIds.Where(id => !validIntegrationIds.Contains(id)).ToList();
                    if (invalidIds.Any())
                    {
                        return BadRequest(GenericResponse<object>.CreateError($"Invalid integration IDs: {string.Join(", ", invalidIds)}", System.Net.HttpStatusCode.BadRequest, "INVALID_INTEGRATIONS"));
                    }
                }

                var result = await _scheduledPostingService.ScheduleRecurringContentAsync(
                    contentId,
                    request.StartDate,
                    request.ScheduledTime,
                    request.Timezone,
                    request.RepeatType,
                    request.RepeatInterval,
                    request.RepeatUntil,
                    request.IntegrationIds);

                return Ok(GenericResponse<ContentResponseDto>.CreateSuccess(result, "Recurring content scheduled successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(GenericResponse<object>.CreateError(ex.Message, System.Net.HttpStatusCode.BadRequest, "SCHEDULE_ERROR"));
            }
        }

        /// <summary>
        /// Cancel a scheduled posting
        /// </summary>
        [HttpDelete("schedule/{scheduleId}")]
        public async Task<IActionResult> CancelSchedule(Guid scheduleId)
        {
            try
            {
                var success = await _scheduledPostingService.CancelScheduleAsync(scheduleId);

                if (!success)
                {
                    return NotFound(GenericResponse<object>.CreateError("Schedule not found", System.Net.HttpStatusCode.NotFound, "NOT_FOUND"));
                }

                return Ok(GenericResponse<object>.CreateSuccess(null, "Schedule cancelled successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(GenericResponse<object>.CreateError(ex.Message, System.Net.HttpStatusCode.BadRequest, "CANCEL_ERROR"));
            }
        }

        /// <summary>
        /// Update a scheduled posting
        /// </summary>
        [HttpPut("schedule/{scheduleId}")]
        public async Task<IActionResult> UpdateSchedule(
            Guid scheduleId,
            [FromBody] UpdateScheduleRequest request)
        {
            try
            {
                var success = await _scheduledPostingService.UpdateScheduleAsync(
                    scheduleId,
                    request.ScheduledDate,
                    request.ScheduledTime);

                if (!success)
                {
                    return NotFound(GenericResponse<object>.CreateError("Schedule not found", System.Net.HttpStatusCode.NotFound, "NOT_FOUND"));
                }

                return Ok(GenericResponse<object>.CreateSuccess(null, "Schedule updated successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(GenericResponse<object>.CreateError(ex.Message, System.Net.HttpStatusCode.BadRequest, "UPDATE_ERROR"));
            }
        }

        /// <summary>
        /// Get upcoming scheduled posts
        /// </summary>
        [HttpGet("upcoming")]
        public async Task<IActionResult> GetUpcomingSchedules([FromQuery] int limit = 50)
        {
            try
            {
                var schedules = await _scheduledPostingService.GetUpcomingSchedulesAsync(limit);
                return Ok(GenericResponse<IEnumerable<Data.Model.ContentCalendar>>.CreateSuccess(schedules, "Upcoming schedules retrieved successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(GenericResponse<object>.CreateError(ex.Message, System.Net.HttpStatusCode.BadRequest, "RETRIEVE_ERROR"));
            }
        }
    }

    public class ScheduleContentRequest
    {
        public DateTime ScheduledDate { get; set; }
        public TimeSpan? ScheduledTime { get; set; }
        public string Timezone { get; set; } = "UTC";
        public List<Guid> IntegrationIds { get; set; } = new();
    }

    public class ScheduleRecurringContentRequest
    {
        public DateTime StartDate { get; set; }
        public TimeSpan? ScheduledTime { get; set; }
        public string Timezone { get; set; } = "UTC";
        public RepeatTypeEnum RepeatType { get; set; }
        public int RepeatInterval { get; set; } = 1;
        public DateTime? RepeatUntil { get; set; }
        public List<Guid> IntegrationIds { get; set; } = new();
    }

    public class UpdateScheduleRequest
    {
        public DateTime ScheduledDate { get; set; }
        public TimeSpan? ScheduledTime { get; set; }
    }
}