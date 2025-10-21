using AISAM.Common.Dtos;
using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface IConversationRepository
    {
        Task<Conversation?> GetByIdAsync(Guid id);
        Task<IEnumerable<Conversation>> GetByProfileIdAsync(Guid profileId, PaginationRequest request);
        Task<Conversation> CreateAsync(Conversation conversation);
        Task UpdateAsync(Conversation conversation);
        Task<bool> SoftDeleteAsync(Guid id);
        Task<Conversation?> GetActiveConversationAsync(Guid profileId, Guid? brandId, Guid? productId, int adType);
    }
}