using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Response;

namespace AISAM.Services.IServices
{
    public interface IConversationService
    {
        Task<PagedResult<ConversationResponseDto>> GetUserConversationsAsync(Guid userId, PaginationRequest request);
        Task<ConversationDetailDto?> GetConversationByIdAsync(Guid conversationId, Guid userId);
        Task<bool> DeleteConversationAsync(Guid conversationId, Guid userId);
        Task<ConversationResponseDto?> CreateOrGetConversationAsync(Guid userId, Guid? brandId, Guid? productId, int adType, string? title = null);
    }
}