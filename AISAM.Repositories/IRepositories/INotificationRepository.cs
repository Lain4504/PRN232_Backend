using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface INotificationRepository
    {
        Task<Notification> CreateAsync(Notification notification);
    }
}

