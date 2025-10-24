using AISAM.API.Utils;
using AISAM.Common;
using AISAM.Common.Dtos.Response;
using AISAM.Data.Enumeration;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using AISAM.Services.Helper;
using Microsoft.AspNetCore.Mvc;
using AISAM.API.Middleware;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/content-calendar")]
    public class ContentCalendarController : ControllerBase
    {
        private readonly IScheduledPostingService _scheduledPostingService;
        private readonly IContentService _contentService;
        private readonly ISocialIntegrationRepository _socialIntegrationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly ITeamMemberRepository _teamMemberRepository;
        private readonly IProfileRepository _profileRepository;
        private readonly RolePermissionConfig _rolePermissionConfig;

        public ContentCalendarController(
            IScheduledPostingService scheduledPostingService,
            IContentService contentService,
            ISocialIntegrationRepository socialIntegrationRepository,
            IUserRepository userRepository,
            IBrandRepository brandRepository,
            ITeamMemberRepository teamMemberRepository,
            IProfileRepository profileRepository,
            RolePermissionConfig rolePermissionConfig)
        {
            _scheduledPostingService = scheduledPostingService;
            _contentService = contentService;
            _socialIntegrationRepository = socialIntegrationRepository;
            _userRepository = userRepository;
            _brandRepository = brandRepository;
            _teamMemberRepository = teamMemberRepository;
            _profileRepository = profileRepository;
            _rolePermissionConfig = rolePermissionConfig;
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
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var content = await _contentService.GetContentByIdAsync(contentId, userId);

                if (content == null)
                {
                    return NotFound(GenericResponse<object>.CreateError("Content not found", System.Net.HttpStatusCode.NotFound, "NOT_FOUND"));
                }

                // Validate integration IDs
                if (request.IntegrationIds != null && request.IntegrationIds.Any())
                {
                    var userIntegrations = await _socialIntegrationRepository.GetByUserIdAsync(userId);
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
                    request.IntegrationIds,
                    userId);

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
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var content = await _contentService.GetContentByIdAsync(contentId, userId);

                if (content == null)
                {
                    return NotFound(GenericResponse<object>.CreateError("Content not found", System.Net.HttpStatusCode.NotFound, "NOT_FOUND"));
                }

                // Validate integration IDs
                if (request.IntegrationIds != null && request.IntegrationIds.Any())
                {
                    var userIntegrations = await _socialIntegrationRepository.GetByUserIdAsync(userId);
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
                    request.IntegrationIds,
                    userId);

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
        public async Task<IActionResult> GetUpcomingSchedules([FromQuery] int limit = 50, [FromQuery] Guid? brandId = null)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                IEnumerable<Data.Model.ContentCalendar> schedules;

                if (brandId.HasValue)
                {
                    // Check if user has permission to view schedules for this brand
                    var canView = await CanUserPerformActionAsync(userId, "VIEW_POSTS", brandId.Value);
                    if (!canView)
                    {
                        return Unauthorized(GenericResponse<object>.CreateError("You are not allowed to view schedules for this brand", System.Net.HttpStatusCode.Unauthorized, "UNAUTHORIZED"));
                    }

                    schedules = await _scheduledPostingService.GetUpcomingSchedulesByBrandAsync(brandId.Value, limit);
                }
                else
                {
                    schedules = await _scheduledPostingService.GetUpcomingSchedulesAsync(limit);
                }

                return Ok(GenericResponse<IEnumerable<Data.Model.ContentCalendar>>.CreateSuccess(schedules, "Upcoming schedules retrieved successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(GenericResponse<object>.CreateError(ex.Message, System.Net.HttpStatusCode.BadRequest, "RETRIEVE_ERROR"));
            }
        }

        /// <summary>
        /// Get scheduled posts for a specific team
        /// </summary>
        [HttpGet("team/{teamId}")]
        public async Task<IActionResult> GetTeamSchedules(Guid teamId, [FromQuery] int limit = 50)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                
                // TODO: Add team membership verification
                // For now, we'll trust the frontend to handle authorization
                
                var schedules = await _scheduledPostingService.GetTeamSchedulesAsync(teamId, limit);
                
                // Convert to DTOs with content and brand information
                var scheduleDtos = schedules.Select(schedule => 
                {
                    var contentTitle = schedule.Content?.Title ?? "Untitled Content";
                    var brandName = schedule.Content?.Brand?.Name ?? "Unknown Brand";
                    
                    return ContentCalendarResponseDto.FromModel(schedule, contentTitle, brandName);
                });

                return Ok(GenericResponse<IEnumerable<ContentCalendarResponseDto>>.CreateSuccess(scheduleDtos, "Team schedules retrieved successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(GenericResponse<object>.CreateError(ex.Message, System.Net.HttpStatusCode.BadRequest, "RETRIEVE_ERROR"));
            }
        }

        /// <summary>
        /// Check if user can perform action with required permission
        /// </summary>
        private async Task<bool> CanUserPerformActionAsync(Guid userId, string permission, Guid? brandId = null)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            // If brandId is provided, check if user is brand owner or team member
            if (brandId.HasValue)
            {
                var brand = await _brandRepository.GetByIdAsync(brandId.Value);
                if (brand == null) return false;

                // Check direct ownership through user's profiles
                var userProfiles = await _profileRepository.GetByUserIdAsync(userId);
                if (userProfiles?.Any(p => p.Id == brand.ProfileId) == true)
                {
                    return true; // User owns this brand directly
                }

                // If brand's profile is Free type, only owner can access
                var brandProfile = await _profileRepository.GetByIdAsync(brand.ProfileId);
                if (brandProfile?.ProfileType == ProfileTypeEnum.Free)
                {
                    return false; // Free profiles don't have team features
                }

                // For Basic/Pro profiles: check team member access
                var teamMember = await _teamMemberRepository.GetByUserIdAndBrandAsync(userId, brandId.Value);
                if (teamMember == null) return false;

                return _rolePermissionConfig.HasCustomPermission(teamMember.Permissions, permission);
            }

            // Fallback: check if user has permission in any team membership
            var userTeamMembers = await _teamMemberRepository.GetByUserIdWithBrandsAsync(userId);
            if (userTeamMembers != null && userTeamMembers.Any())
            {
                return userTeamMembers.Any(tm => _rolePermissionConfig.HasCustomPermission(tm.Permissions, permission));
            }

            return false;
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