using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Response;
using AISAM.Data.Enumeration;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;

namespace AISAM.Services.Service
{
    public class ConversationService : IConversationService
    {
        private readonly IConversationRepository _conversationRepository;

        public ConversationService(IConversationRepository conversationRepository)
        {
            _conversationRepository = conversationRepository;
        }

        public async Task<PagedResult<ConversationResponseDto>> GetUserConversationsAsync(Guid userId, PaginationRequest request)
        {
            var conversations = await _conversationRepository.GetByProfileIdAsync(userId, request);

            var result = new PagedResult<ConversationResponseDto>
            {
                Data = conversations.Select(MapToResponseDto).ToList(),
                TotalCount = conversations.Count(), // This is approximate, would need a proper count query
                Page = request.Page,
                PageSize = request.PageSize
            };

            return result;
        }

        public async Task<ConversationDetailDto?> GetConversationByIdAsync(Guid conversationId, Guid userId)
        {
            var conversation = await _conversationRepository.GetByIdAsync(conversationId);

            // Check if conversation exists and belongs to user
            if (conversation == null || conversation.ProfileId != userId)
                return null;

            return MapToDetailDto(conversation);
        }

        public async Task<bool> DeleteConversationAsync(Guid conversationId, Guid userId)
        {
            var conversation = await _conversationRepository.GetByIdAsync(conversationId);

            // Check if conversation exists and belongs to user
            if (conversation == null || conversation.ProfileId != userId)
                return false;

            return await _conversationRepository.SoftDeleteAsync(conversationId);
        }

        public async Task<ConversationResponseDto?> CreateOrGetConversationAsync(Guid userId, Guid? brandId, Guid? productId, int adType, string? title = null)
        {
            // Try to find existing active conversation
            var existingConversation = await _conversationRepository.GetActiveConversationAsync(
                userId, brandId, productId, adType);

            if (existingConversation != null)
            {
                return MapToResponseDto(existingConversation);
            }

            // Create new conversation
            var conversation = new Conversation
            {
                ProfileId = userId,
                BrandId = brandId,
                ProductId = productId,
                AdType = (AdTypeEnum)adType,
                Title = title,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _conversationRepository.CreateAsync(conversation);
            return MapToResponseDto(created);
        }

        private static ConversationResponseDto MapToResponseDto(Conversation conversation)
        {
            var lastMessage = conversation.ChatMessages.LastOrDefault();

            return new ConversationResponseDto
            {
                Id = conversation.Id,
                ProfileId = conversation.ProfileId,
                BrandId = conversation.BrandId,
                BrandName = conversation.Brand?.Name,
                ProductId = conversation.ProductId,
                ProductName = conversation.Product?.Name,
                AdType = conversation.AdType.ToString(),
                Title = conversation.Title,
                IsActive = conversation.IsActive,
                CreatedAt = conversation.CreatedAt,
                UpdatedAt = conversation.UpdatedAt,
                LastMessage = lastMessage?.Message,
                LastMessageAt = lastMessage?.CreatedAt,
                MessageCount = conversation.ChatMessages.Count
            };
        }

        private static ConversationDetailDto MapToDetailDto(Conversation conversation)
        {
            return new ConversationDetailDto
            {
                Id = conversation.Id,
                ProfileId = conversation.ProfileId,
                BrandId = conversation.BrandId,
                BrandName = conversation.Brand?.Name,
                ProductId = conversation.ProductId,
                ProductName = conversation.Product?.Name,
                AdType = conversation.AdType.ToString(),
                Title = conversation.Title,
                IsActive = conversation.IsActive,
                CreatedAt = conversation.CreatedAt,
                UpdatedAt = conversation.UpdatedAt,
                Messages = conversation.ChatMessages.Select(MapToChatMessageDto).ToList()
            };
        }

        private static ChatMessageDto MapToChatMessageDto(ChatMessage message)
        {
            return new ChatMessageDto
            {
                Id = message.Id,
                SenderType = message.SenderType.ToString(),
                Message = message.Message,
                AiGenerationId = message.AiGenerationId,
                ContentId = message.ContentId,
                CreatedAt = message.CreatedAt
            };
        }
    }
}