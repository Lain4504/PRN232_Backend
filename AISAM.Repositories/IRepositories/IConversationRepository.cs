using AISAM.Common.Dtos;
using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface IConversationRepository
    {
        Task<Conversation?> GetByIdAsync(Guid id);
        Task<IEnumerable<Conversation>> GetByUserIdAsync(Guid userId, PaginationRequest request);
        Task<Conversation> CreateAsync(Conversation conversation);
        Task UpdateAsync(Conversation conversation);
        Task<bool> SoftDeleteAsync(Guid id);
        Task<Conversation?> GetActiveConversationAsync(Guid userId, Guid? brandId, Guid? productId, int adType);
    }
}