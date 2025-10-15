using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface INotificationsRepository
    {
        Task<Notification> AddAsync(Notification entity, CancellationToken ct);
    }
}


