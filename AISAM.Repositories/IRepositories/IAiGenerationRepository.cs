using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface IAiGenerationRepository
    {
        Task<AiGeneration?> GetByIdAsync(Guid id);
        Task<IEnumerable<AiGeneration>> GetByContentIdAsync(Guid contentId);
        Task<AiGeneration> CreateAsync(AiGeneration aiGeneration);
        Task UpdateAsync(AiGeneration aiGeneration);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}