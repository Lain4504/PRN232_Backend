using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace AISAM.Repositories.Repository
{
    public class AiGenerationRepository : IAiGenerationRepository
    {
        private readonly AisamContext _context;

        public AiGenerationRepository(AisamContext context)
        {
            _context = context;
        }

        public async Task<AiGeneration?> GetByIdAsync(Guid id)
        {
            return await _context.AiGenerations
                .Include(ag => ag.Content)
                .FirstOrDefaultAsync(ag => ag.Id == id && !ag.IsDeleted);
        }

        public async Task<IEnumerable<AiGeneration>> GetByContentIdAsync(Guid contentId)
        {
            return await _context.AiGenerations
                .Include(ag => ag.Content)
                .Where(ag => ag.ContentId == contentId && !ag.IsDeleted)
                .OrderByDescending(ag => ag.CreatedAt)
                .ToListAsync();
        }

        public async Task<AiGeneration> CreateAsync(AiGeneration aiGeneration)
        {
            aiGeneration.CreatedAt = DateTime.UtcNow;
            aiGeneration.UpdatedAt = DateTime.UtcNow;

            _context.AiGenerations.Add(aiGeneration);
            await _context.SaveChangesAsync();
            return aiGeneration;
        }

        public async Task UpdateAsync(AiGeneration aiGeneration)
        {
            aiGeneration.UpdatedAt = DateTime.UtcNow;
            _context.AiGenerations.Update(aiGeneration);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var aiGeneration = await _context.AiGenerations
                .FirstOrDefaultAsync(ag => ag.Id == id && !ag.IsDeleted);

            if (aiGeneration != null)
            {
                aiGeneration.IsDeleted = true;
                aiGeneration.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.AiGenerations
                .AnyAsync(ag => ag.Id == id && !ag.IsDeleted);
        }
    }
}