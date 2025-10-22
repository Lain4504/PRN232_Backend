using AISAM.Data.Model;
using AISAM.Data.Enumeration;

namespace AISAM.Repositories.IRepositories
{
    public interface IContentCalendarRepository
    {
        Task<ContentCalendar?> GetByIdAsync(Guid id);
        Task<ContentCalendar> CreateAsync(ContentCalendar schedule);
        Task UpdateAsync(ContentCalendar schedule);
        Task DeleteAsync(Guid id);
        Task<IEnumerable<ContentCalendar>> GetDueSchedulesAsync(DateTime utcNow, int limit = 50);
        Task<IEnumerable<ContentCalendar>> GetRecurringSchedulesAsync(DateTime utcNow, int limit = 50);
        Task<IEnumerable<ContentCalendar>> GetSchedulesByContentIdAsync(Guid contentId);
        Task<IEnumerable<ContentCalendar>> GetActiveSchedulesAsync(DateTime fromDate, DateTime toDate);
        Task UpdateNextScheduledDateAsync(Guid id, DateTime nextDate);
        Task DeactivateScheduleAsync(Guid id);
        Task<IEnumerable<ContentCalendar>> GetByTeamIdAsync(Guid teamId, int limit = 50);
        Task<IEnumerable<ContentCalendar>> GetUpcomingSchedulesByBrandAsync(Guid brandId, DateTime fromDate, DateTime toDate, int limit = 50);
    }
}

