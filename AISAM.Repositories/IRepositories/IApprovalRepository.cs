using AISAM.Data.Enumeration;
using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface IApprovalRepository
    {
        Task<Approval?> GetByIdAsync(Guid id);
        Task<Approval?> GetByIdIncludingDeletedAsync(Guid id);
        Task<IEnumerable<Approval>> GetByContentIdAsync(Guid contentId);
        Task<IEnumerable<Approval>> GetByApproverIdAsync(Guid approverId);
        
        Task<(IEnumerable<Approval> Items, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            string? searchTerm,
            string? sortBy,
            bool sortDescending,
            ContentStatusEnum? status,
            Guid? contentId,
            Guid? approverId,
            bool onlyDeleted);
            
        Task<Approval> CreateAsync(Approval approval);
        Task UpdateAsync(Approval approval);
        Task DeleteAsync(Guid id);
        Task HardDeleteAsync(Guid id);
        Task RestoreAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> HasPendingApprovalAsync(Guid contentId);
        Task<int> GetPendingCountAsync(Guid approverId);
    }
}