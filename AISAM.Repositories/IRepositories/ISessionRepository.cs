using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface ISessionRepository
    {
        Task<Session?> GetByIdAsync(Guid id);
        Task<Session?> GetByRefreshTokenAsync(string refreshToken);
        Task<List<Session>> GetActiveSessionsByUserIdAsync(Guid userId);
        Task<Session> CreateAsync(Session session);
        Task UpdateAsync(Session session);
        Task RevokeSessionAsync(Guid sessionId);
        Task RevokeAllUserSessionsAsync(Guid userId);
        Task DeleteExpiredSessionsAsync();
    }
}
