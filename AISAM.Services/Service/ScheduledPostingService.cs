using AISAM.Common.Dtos.Response;
using AISAM.Data.Enumeration;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using Microsoft.Extensions.Logging;

namespace AISAM.Services.Service
{
    public class ScheduledPostingService : IScheduledPostingService
    {
        private readonly IContentCalendarRepository _calendarRepository;
        private readonly IContentService _contentService;
        private readonly ILogger<ScheduledPostingService> _logger;

        public ScheduledPostingService(
            IContentCalendarRepository calendarRepository,
            IContentService contentService,
            ILogger<ScheduledPostingService> logger)
        {
            _calendarRepository = calendarRepository;
            _contentService = contentService;
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

                // Publish to each selected integration
                bool allSuccessful = true;
                foreach (var integrationId in integrationIds)
                {
                    var publishResult = await _contentService.PublishContentAsync(schedule.ContentId, integrationId, schedule.UserId);
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

                // Publish to each selected integration
                bool allSuccessful = true;
                foreach (var integrationId in integrationIds)
                {
                    var publishResult = await _contentService.PublishContentAsync(schedule.ContentId, integrationId, schedule.UserId);
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

        public async Task<ContentResponseDto> ScheduleContentAsync(Guid contentId, DateTime scheduledDate, TimeSpan? scheduledTime = null, string timezone = "UTC", List<Guid>? integrationIds = null)
        {
            var userId = Guid.Empty; // TODO: Get from current user context

            var schedule = new ContentCalendar
            {
                ContentId = contentId,
                ScheduledDate = scheduledDate.Date,
                ScheduledTime = scheduledTime,
                Timezone = timezone,
                RepeatType = RepeatTypeEnum.None,
                IntegrationIds = integrationIds != null && integrationIds.Any() ? System.Text.Json.JsonSerializer.Serialize(integrationIds) : null,
                UserId = userId,
                IsActive = true
            };

            await _calendarRepository.CreateAsync(schedule);

            // Return content with schedule info
            return await _contentService.GetContentByIdAsync(contentId, userId);
        }

        public async Task<ContentResponseDto> ScheduleRecurringContentAsync(Guid contentId, DateTime startDate, TimeSpan? scheduledTime, string timezone, RepeatTypeEnum repeatType, int repeatInterval = 1, DateTime? repeatUntil = null, List<Guid>? integrationIds = null)
        {
            var userId = Guid.Empty; // TODO: Get from current user context

            var schedule = new ContentCalendar
            {
                ContentId = contentId,
                ScheduledDate = startDate.Date,
                ScheduledTime = scheduledTime,
                Timezone = timezone,
                RepeatType = repeatType,
                RepeatInterval = repeatInterval,
                RepeatUntil = repeatUntil,
                NextScheduledDate = startDate.Date,
                IntegrationIds = integrationIds != null && integrationIds.Any() ? System.Text.Json.JsonSerializer.Serialize(integrationIds) : null,
                UserId = userId,
                IsActive = true
            };

            await _calendarRepository.CreateAsync(schedule);

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

            schedule.ScheduledDate = newScheduledDate.Date;
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

        public Task<DateTime?> CalculateNextScheduledDateAsync(DateTime currentDate, RepeatTypeEnum repeatType, int interval)
        {
            DateTime? nextDate = null;

            switch (repeatType)
            {
                case RepeatTypeEnum.Daily:
                    nextDate = currentDate.AddDays(interval);
                    break;
                case RepeatTypeEnum.Weekly:
                    nextDate = currentDate.AddDays(7 * interval);
                    break;
                case RepeatTypeEnum.Monthly:
                    nextDate = currentDate.AddMonths(interval);
                    break;
                default:
                    nextDate = null;
                    break;
            }

            return Task.FromResult(nextDate);
        }
    }
}