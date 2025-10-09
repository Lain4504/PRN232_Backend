using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class ContentCalendarRepository : IContentCalendarRepository
    {
        private readonly AisamContext _context;

        public ContentCalendarRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<ContentCalendar?> GetByIdAsync(Guid id)
        {
            return await _context.ContentCalendars.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<ContentCalendar> CreateAsync(ContentCalendar schedule)
        {
            _context.ContentCalendars.Add(schedule);
            await _context.SaveChangesAsync();
            return schedule;
        }

        public async Task DeleteAsync(Guid id)
        {
            var schedule = await _context.ContentCalendars.FindAsync(id);
            if (schedule != null)
            {
                _context.ContentCalendars.Remove(schedule);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<ContentCalendar>> GetDueSchedulesAsync(DateTime utcNow, int limit = 50)
        {
            return await _context.ContentCalendars
                .Where(c => !c.IsDeleted && c.ScheduledDate <= utcNow.Date)
                .OrderBy(c => c.ScheduledDate)
                .Take(limit)
                .ToListAsync();
        }
    }
}

