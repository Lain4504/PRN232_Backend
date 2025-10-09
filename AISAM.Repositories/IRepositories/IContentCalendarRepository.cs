using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface IContentCalendarRepository
    {
        Task<ContentCalendar?> GetByIdAsync(Guid id);
        Task<ContentCalendar> CreateAsync(ContentCalendar schedule);
        Task DeleteAsync(Guid id);
        Task<IEnumerable<ContentCalendar>> GetDueSchedulesAsync(DateTime utcNow, int limit = 50);
    }
}

