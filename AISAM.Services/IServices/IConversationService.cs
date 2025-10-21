using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Response;

namespace AISAM.Services.IServices
{
    public interface IConversationService
    {
        Task<PagedResult<ConversationResponseDto>> GetUserConversationsAsync(Guid profileId, PaginationRequest request);
        Task<ConversationDetailDto?> GetConversationByIdAsync(Guid conversationId, Guid profileId);
        Task<bool> DeleteConversationAsync(Guid conversationId, Guid profileId);
        Task<ConversationResponseDto?> CreateOrGetConversationAsync(Guid profileId, Guid? brandId, Guid? productId, int adType, string? title = null);
    }
}