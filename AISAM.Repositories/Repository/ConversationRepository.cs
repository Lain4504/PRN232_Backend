using AISAM.Common.Dtos;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class ConversationRepository : IConversationRepository
    {
        private readonly AisamContext _context;

        public ConversationRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<Conversation?> GetByIdAsync(Guid id)
        {
            return await _context.Conversations
                .Include(c => c.ChatMessages.OrderBy(cm => cm.CreatedAt))
                    .ThenInclude(cm => cm.AiGeneration)
                .Include(c => c.Brand)
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        }

        public async Task<IEnumerable<Conversation>> GetByProfileIdAsync(Guid profileId, PaginationRequest request)
        {
            var query = _context.Conversations
                .Where(c => c.ProfileId == profileId && !c.IsDeleted)
                .Include(c => c.ChatMessages.OrderBy(cm => cm.CreatedAt).Take(1)) // Get first message for preview
                .Include(c => c.Brand)
                .Include(c => c.Product)
                .AsQueryable();

            // Apply sorting
            if (!string.IsNullOrEmpty(request.SortBy))
            {
                query = request.SortBy.ToLower() switch
                {
                    "createdat" => request.SortDescending
                        ? query.OrderByDescending(c => c.CreatedAt)
                        : query.OrderBy(c => c.CreatedAt),
                    "title" => request.SortDescending
                        ? query.OrderByDescending(c => c.Title)
                        : query.OrderBy(c => c.Title),
                    _ => query.OrderByDescending(c => c.CreatedAt)
                };
            }
            else
            {
                query = query.OrderByDescending(c => c.CreatedAt);
            }

            // Apply pagination
            return await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();
        }

        public async Task<Conversation> CreateAsync(Conversation conversation)
        {
            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();
            return conversation;
        }

        public async Task UpdateAsync(Conversation conversation)
        {
            _context.Conversations.Update(conversation);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            var conversation = await _context.Conversations.FindAsync(id);
            if (conversation == null || conversation.IsDeleted)
                return false;

            conversation.IsDeleted = true;
            conversation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Conversation?> GetActiveConversationAsync(Guid profileId, Guid? brandId, Guid? productId, int adType)
        {
            return await _context.Conversations
                .Include(c => c.ChatMessages.OrderBy(cm => cm.CreatedAt))
                    .ThenInclude(cm => cm.AiGeneration)
                .Include(c => c.Brand)
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c =>
                    c.ProfileId == profileId &&
                    c.BrandId == brandId &&
                    c.ProductId == productId &&
                    c.AdType == (AISAM.Data.Enumeration.AdTypeEnum)adType &&
                    c.IsActive &&
                    !c.IsDeleted);
        }
    }
}