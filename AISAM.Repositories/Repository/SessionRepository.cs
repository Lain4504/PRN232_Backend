using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class SessionRepository : ISessionRepository
    {
        private readonly AisamContext _context;

        public SessionRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<Session?> GetByIdAsync(Guid id)
        {
            return await _context.Sessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Session?> GetByRefreshTokenAsync(string refreshToken)
        {
            return await _context.Sessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken && s.IsActive);
        }

        public async Task<List<Session>> GetActiveSessionsByUserIdAsync(Guid userId)
        {
            return await _context.Sessions
                .Where(s => s.UserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<Session> CreateAsync(Session session)
        {
            await _context.Sessions.AddAsync(session);
            await _context.SaveChangesAsync();
            return session;
        }

        public async Task UpdateAsync(Session session)
        {
            _context.Sessions.Update(session);
            await _context.SaveChangesAsync();
        }

        public async Task RevokeSessionAsync(Guid sessionId)
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session != null)
            {
                session.IsActive = false;
                session.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task RevokeAllUserSessionsAsync(Guid userId)
        {
            var sessions = await _context.Sessions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();

            foreach (var session in sessions)
            {
                session.IsActive = false;
                session.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteExpiredSessionsAsync()
        {
            var expiredSessions = await _context.Sessions
                .Where(s => s.ExpiresAt < DateTime.UtcNow || !s.IsActive)
                .ToListAsync();

            _context.Sessions.RemoveRange(expiredSessions);
            await _context.SaveChangesAsync();
        }
    }
}
