using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface IPostRepository
    {
        Task<SocialPost> CreateAsync(SocialPost socialPost);
    }
}