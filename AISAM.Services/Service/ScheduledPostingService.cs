using AISAM.Common.Dtos.Response;
using AISAM.Data.Enumeration;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using AISAM.Services.Helper;
using Microsoft.Extensions.Logging;

namespace AISAM.Services.Service
{
    public class ScheduledPostingService : IScheduledPostingService
    {
        private readonly IContentCalendarRepository _calendarRepository;
        private readonly IContentService _contentService;
        private readonly IProfileRepository _profileRepository;
        private readonly IUserRepository _userRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly ITeamMemberRepository _teamMemberRepository;
        private readonly RolePermissionConfig _rolePermissionConfig;
        private readonly ILogger<ScheduledPostingService> _logger;

        public ScheduledPostingService(
            IContentCalendarRepository calendarRepository,
            IContentService contentService,
            IProfileRepository profileRepository,
            IUserRepository userRepository,
            IBrandRepository brandRepository,
            ITeamMemberRepository teamMemberRepository,
            RolePermissionConfig rolePermissionConfig,
            ILogger<ScheduledPostingService> logger)
        {
            _calendarRepository = calendarRepository;
            _contentService = contentService;
            _profileRepository = profileRepository;
            _userRepository = userRepository;
            _brandRepository = brandRepository;
            _teamMemberRepository = teamMemberRepository;
            _rolePermissionConfig = rolePermissionConfig;
            _logger = logger;
        }

        public async Task ProcessDueSchedulesAsync()
        {
            try
            {
                var utcNow = DateTime.UtcNow;
                var dueSchedules = await _calendarRepository.GetDueSchedulesAsync(utcNow, 100);

                foreach (var schedule in dueSchedules)
                {
                    try
                    {
                        await ProcessScheduleAsync(schedule);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to process schedule {schedule.Id}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing due schedules");
            }
        }

        public async Task ProcessRecurringSchedulesAsync()
        {
            try
            {
                var utcNow = DateTime.UtcNow;
                var recurringSchedules = await _calendarRepository.GetRecurringSchedulesAsync(utcNow, 50);

                foreach (var schedule in recurringSchedules)
                {
                    try
                    {
                        await ProcessRecurringScheduleAsync(schedule);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to process recurring schedule {schedule.Id}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing recurring schedules");
            }
        }

        private async Task ProcessScheduleAsync(ContentCalendar schedule)
        {
            _logger.LogInformation($"Processing schedule {schedule.Id} for content {schedule.ContentId}");

            try
            {
                // Parse integration IDs from JSON
                List<Guid> integrationIds = new();
                if (!string.IsNullOrEmpty(schedule.IntegrationIds))
                {
                    integrationIds = System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(schedule.IntegrationIds) ?? new();
                }

                if (!integrationIds.Any())
                {
                    _logger.LogWarning($"No integrations selected for schedule {schedule.Id}");
                    await _calendarRepository.DeactivateScheduleAsync(schedule.Id);
                    return;
                }

                // Get the userId from the profile
                var profile = await _profileRepository.GetByIdAsync(schedule.ProfileId);
                if (profile == null)
                {
                    _logger.LogWarning($"Profile {schedule.ProfileId} not found for schedule {schedule.Id}");
                    await _calendarRepository.DeactivateScheduleAsync(schedule.Id);
                    return;
                }

                // Publish to each selected integration
                bool allSuccessful = true;
                foreach (var integrationId in integrationIds)
                {
                    var publishResult = await _contentService.PublishContentAsync(schedule.ContentId, integrationId, profile.UserId);
                    if (publishResult == null)
                    {
                        _logger.LogWarning($"Failed to publish content {schedule.ContentId} to integration {integrationId}");
                        allSuccessful = false;
                    }
                }

                if (allSuccessful)
                {
                    // Deactivate one-time schedule
                    await _calendarRepository.DeactivateScheduleAsync(schedule.Id);
                    _logger.LogInformation($"Successfully processed schedule {schedule.Id}");
                }
                else
                {
                    _logger.LogWarning($"Some integrations failed for schedule {schedule.Id}, keeping schedule active for retry");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing schedule {schedule.Id}: {ex.Message}");
            }
        }

        private async Task ProcessRecurringScheduleAsync(ContentCalendar schedule)
        {
            _logger.LogInformation($"Processing recurring schedule {schedule.Id} for content {schedule.ContentId}");

            try
            {
                // Parse integration IDs from JSON
                List<Guid> integrationIds = new();
                if (!string.IsNullOrEmpty(schedule.IntegrationIds))
                {
                    integrationIds = System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(schedule.IntegrationIds) ?? new();
                }

                if (!integrationIds.Any())
                {
                    _logger.LogWarning($"No integrations selected for recurring schedule {schedule.Id}");
                    await _calendarRepository.DeactivateScheduleAsync(schedule.Id);
                    return;
                }

                // Get the userId from the profile
                var profile = await _profileRepository.GetByIdAsync(schedule.ProfileId);
                if (profile == null)
                {
                    _logger.LogWarning($"Profile {schedule.ProfileId} not found for recurring schedule {schedule.Id}");
                    await _calendarRepository.DeactivateScheduleAsync(schedule.Id);
                    return;
                }

                // Publish to each selected integration
                bool allSuccessful = true;
                foreach (var integrationId in integrationIds)
                {
                    var publishResult = await _contentService.PublishContentAsync(schedule.ContentId, integrationId, profile.UserId);
                    if (publishResult == null)
                    {
                        _logger.LogWarning($"Failed to publish content {schedule.ContentId} to integration {integrationId} for recurring schedule");
                        allSuccessful = false;
                    }
                }

                if (allSuccessful)
                {
                    // Calculate next scheduled date
                    var nextDate = await CalculateNextScheduledDateAsync(schedule.ScheduledDate, schedule.RepeatType, schedule.RepeatInterval);

                    if (nextDate.HasValue && (!schedule.RepeatUntil.HasValue || nextDate.Value <= schedule.RepeatUntil.Value))
                    {
                        // Update next scheduled date
                        await _calendarRepository.UpdateNextScheduledDateAsync(schedule.Id, nextDate.Value);
                        _logger.LogInformation($"Updated recurring schedule {schedule.Id} to next date {nextDate.Value}");
                    }
                    else
                    {
                        // Deactivate if no more occurrences or reached end date
                        await _calendarRepository.DeactivateScheduleAsync(schedule.Id);
                        _logger.LogInformation($"Deactivated recurring schedule {schedule.Id} - no more occurrences");
                    }
                }
                else
                {
                    _logger.LogWarning($"Some integrations failed for recurring schedule {schedule.Id}, keeping schedule active for retry");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing recurring schedule {schedule.Id}: {ex.Message}");
            }
        }

        public async Task<ContentResponseDto> ScheduleContentAsync(Guid contentId, DateTime scheduledDate, TimeSpan? scheduledTime = null, string timezone = "UTC", List<Guid>? integrationIds = null, Guid userId = default)
        {
            // Validate user exists
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            // Get content to validate access and get brandId
            var content = await _contentService.GetContentByIdAsync(contentId, userId);
            if (content == null)
            {
                throw new ArgumentException("Content not found");
            }

            // Check if user has permission to schedule content
            var canSchedule = await CanUserPerformActionAsync(userId, "CREATE_CONTENT", content.BrandId);
            if (!canSchedule)
            {
                throw new UnauthorizedAccessException("You are not allowed to schedule content");
            }

            // Get the user's profile to get the profileId
            var userProfiles = await _profileRepository.GetByUserIdAsync(userId);
            var userProfile = userProfiles?.FirstOrDefault();
            if (userProfile == null)
            {
                throw new ArgumentException("User profile not found");
            }

            var schedule = new ContentCalendar
            {
                ContentId = contentId,
                ScheduledDate = DateTime.SpecifyKind(scheduledDate.Date, DateTimeKind.Utc),
                ScheduledTime = scheduledTime,
                Timezone = timezone,
                RepeatType = RepeatTypeEnum.None,
                IntegrationIds = integrationIds != null && integrationIds.Any() ? System.Text.Json.JsonSerializer.Serialize(integrationIds) : null,
                ProfileId = userProfile.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _calendarRepository.CreateAsync(schedule);

            _logger.LogInformation("User {UserId} scheduled content {ContentId} for {ScheduledDate}", userId, contentId, scheduledDate);

            // Return content with schedule info
            return await _contentService.GetContentByIdAsync(contentId, userId);
        }

        public async Task<ContentResponseDto> ScheduleRecurringContentAsync(Guid contentId, DateTime startDate, TimeSpan? scheduledTime, string timezone, RepeatTypeEnum repeatType, int repeatInterval = 1, DateTime? repeatUntil = null, List<Guid>? integrationIds = null, Guid userId = default)
        {
            // Validate user exists
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            // Get content to validate access and get brandId
            var content = await _contentService.GetContentByIdAsync(contentId, userId);
            if (content == null)
            {
                throw new ArgumentException("Content not found");
            }

            // Check if user has permission to schedule content
            var canSchedule = await CanUserPerformActionAsync(userId, "CREATE_CONTENT", content.BrandId);
            if (!canSchedule)
            {
                throw new UnauthorizedAccessException("You are not allowed to schedule content");
            }

            // Get the user's profile to get the profileId
            var userProfiles = await _profileRepository.GetByUserIdAsync(userId);
            var userProfile = userProfiles?.FirstOrDefault();
            if (userProfile == null)
            {
                throw new ArgumentException("User profile not found");
            }

            var schedule = new ContentCalendar
            {
                ContentId = contentId,
                ScheduledDate = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Utc),
                ScheduledTime = scheduledTime,
                Timezone = timezone,
                RepeatType = repeatType,
                RepeatInterval = repeatInterval,
                RepeatUntil = repeatUntil.HasValue ? DateTime.SpecifyKind(repeatUntil.Value, DateTimeKind.Utc) : null,
                NextScheduledDate = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Utc),
                IntegrationIds = integrationIds != null && integrationIds.Any() ? System.Text.Json.JsonSerializer.Serialize(integrationIds) : null,
                ProfileId = userProfile.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _calendarRepository.CreateAsync(schedule);

            _logger.LogInformation("User {UserId} scheduled recurring content {ContentId} starting {StartDate}", userId, contentId, startDate);

            // Return content with schedule info
            return await _contentService.GetContentByIdAsync(contentId, userId);
        }

        public async Task<bool> CancelScheduleAsync(Guid scheduleId)
        {
            await _calendarRepository.DeactivateScheduleAsync(scheduleId);
            return true;
        }

        public async Task<bool> UpdateScheduleAsync(Guid scheduleId, DateTime newScheduledDate, TimeSpan? newScheduledTime = null)
        {
            var schedule = await _calendarRepository.GetByIdAsync(scheduleId);
            if (schedule == null) return false;

            schedule.ScheduledDate = DateTime.SpecifyKind(newScheduledDate.Date, DateTimeKind.Utc);
            schedule.ScheduledTime = newScheduledTime;
            schedule.UpdatedAt = DateTime.UtcNow;

            await _calendarRepository.UpdateAsync(schedule);
            return true;
        }

        public async Task<IEnumerable<ContentCalendar>> GetUpcomingSchedulesAsync(int limit = 50)
        {
            var utcNow = DateTime.UtcNow;
            var futureDate = utcNow.AddDays(30); // Get schedules for next 30 days

            return await _calendarRepository.GetActiveSchedulesAsync(utcNow, futureDate);
        }

        public async Task<IEnumerable<ContentCalendar>> GetUpcomingSchedulesByBrandAsync(Guid brandId, int limit = 50)
        {
            var utcNow = DateTime.UtcNow;
            var futureDate = utcNow.AddDays(30); // Get schedules for next 30 days

            return await _calendarRepository.GetUpcomingSchedulesByBrandAsync(brandId, utcNow, futureDate, limit);
        }

        public Task<DateTime?> CalculateNextScheduledDateAsync(DateTime currentDate, RepeatTypeEnum repeatType, int interval)
        {
            DateTime? nextDate = null;

            switch (repeatType)
            {
                case RepeatTypeEnum.Daily:
                    nextDate = DateTime.SpecifyKind(currentDate.AddDays(interval), DateTimeKind.Utc);
                    break;
                case RepeatTypeEnum.Weekly:
                    nextDate = DateTime.SpecifyKind(currentDate.AddDays(7 * interval), DateTimeKind.Utc);
                    break;
                case RepeatTypeEnum.Monthly:
                    nextDate = DateTime.SpecifyKind(currentDate.AddMonths(interval), DateTimeKind.Utc);
                    break;
                default:
                    nextDate = null;
                    break;
            }

            return Task.FromResult(nextDate);
        }

        public async Task<IEnumerable<ContentCalendar>> GetTeamSchedulesAsync(Guid teamId, int limit = 50)
        {
            return await _calendarRepository.GetByTeamIdAsync(teamId, limit);
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
}