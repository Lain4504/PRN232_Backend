using AISAM.Data.Model;
using AISAM.Data.Enumeration;
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

        public async Task UpdateAsync(ContentCalendar schedule)
        {
            schedule.UpdatedAt = DateTime.UtcNow;
            _context.ContentCalendars.Update(schedule);
            await _context.SaveChangesAsync();
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
                .Where(c => !c.IsDeleted && c.IsActive &&
                           ((c.ScheduledDate <= utcNow.Date && c.ScheduledTime == null) ||
                            (c.ScheduledDate == utcNow.Date && c.ScheduledTime.HasValue && c.ScheduledTime.Value <= utcNow.TimeOfDay)))
                .OrderBy(c => c.ScheduledDate)
                .ThenBy(c => c.ScheduledTime)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<IEnumerable<ContentCalendar>> GetRecurringSchedulesAsync(DateTime utcNow, int limit = 50)
        {
            return await _context.ContentCalendars
                .Where(c => !c.IsDeleted && c.IsActive &&
                           c.RepeatType != RepeatTypeEnum.None &&
                           c.NextScheduledDate.HasValue &&
                           c.NextScheduledDate <= utcNow)
                .OrderBy(c => c.NextScheduledDate)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<IEnumerable<ContentCalendar>> GetSchedulesByContentIdAsync(Guid contentId)
        {
            return await _context.ContentCalendars
                .Where(c => c.ContentId == contentId && !c.IsDeleted)
                .OrderBy(c => c.ScheduledDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<ContentCalendar>> GetActiveSchedulesAsync(DateTime fromDate, DateTime toDate)
        {
            return await _context.ContentCalendars
                .Include(c => c.Content)
                    .ThenInclude(content => content.Brand)
                .Where(c => !c.IsDeleted && c.IsActive &&
                           c.ScheduledDate >= fromDate.Date && c.ScheduledDate <= toDate.Date)
                .OrderBy(c => c.ScheduledDate)
                .ThenBy(c => c.ScheduledTime)
                .ToListAsync();
        }

        public async Task UpdateNextScheduledDateAsync(Guid id, DateTime nextDate)
        {
            var schedule = await _context.ContentCalendars.FindAsync(id);
            if (schedule != null)
            {
                schedule.NextScheduledDate = nextDate;
                schedule.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeactivateScheduleAsync(Guid id)
        {
            var schedule = await _context.ContentCalendars.FindAsync(id);
            if (schedule != null)
            {
                schedule.IsActive = false;
                schedule.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<ContentCalendar>> GetByTeamIdAsync(Guid teamId, int limit = 50)
        {
            return await _context.ContentCalendars
                .Include(c => c.Content)
                    .ThenInclude(content => content.Brand)
                .Where(c => !c.IsDeleted && 
                           c.Content.Brand.TeamBrands.Any(tb => tb.TeamId == teamId))
                .OrderBy(c => c.ScheduledDate)
                .ThenBy(c => c.ScheduledTime)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<IEnumerable<ContentCalendar>> GetUpcomingSchedulesByBrandAsync(Guid brandId, DateTime fromDate, DateTime toDate, int limit = 50)
        {
            return await _context.ContentCalendars
                .Include(c => c.Content)
                    .ThenInclude(content => content.Brand)
                .Where(c => !c.IsDeleted && c.IsActive &&
                           c.Content.BrandId == brandId &&
                           c.ScheduledDate >= fromDate.Date && c.ScheduledDate <= toDate.Date)
                .OrderBy(c => c.ScheduledDate)
                .ThenBy(c => c.ScheduledTime)
                .Take(limit)
                .ToListAsync();
        }
    }
}

