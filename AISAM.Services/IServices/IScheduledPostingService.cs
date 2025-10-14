using AISAM.Common.Dtos.Response;
using AISAM.Data.Enumeration;
using AISAM.Data.Model;

namespace AISAM.Services.IServices
{
    public interface IScheduledPostingService
    {
        Task ProcessDueSchedulesAsync();
        Task ProcessRecurringSchedulesAsync();
        Task<ContentResponseDto> ScheduleContentAsync(Guid contentId, DateTime scheduledDate, TimeSpan? scheduledTime = null, string timezone = "UTC");
        Task<ContentResponseDto> ScheduleRecurringContentAsync(Guid contentId, DateTime startDate, TimeSpan? scheduledTime, string timezone, RepeatTypeEnum repeatType, int repeatInterval = 1, DateTime? repeatUntil = null);
        Task<bool> CancelScheduleAsync(Guid scheduleId);
        Task<bool> UpdateScheduleAsync(Guid scheduleId, DateTime newScheduledDate, TimeSpan? newScheduledTime = null);
        Task<IEnumerable<ContentCalendar>> GetUpcomingSchedulesAsync(int limit = 50);
        Task<DateTime?> CalculateNextScheduledDateAsync(DateTime currentDate, RepeatTypeEnum repeatType, int interval);
    }
}