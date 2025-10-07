using AISAM.Common.Dtos;
using AISAM.Data.Model;

namespace AISAM.Repositories.IRepositories
{
    public interface ITeamMemberRepository
    {
        Task<PagedResult<TeamMember>> GetPagedAsync(PaginationRequest request);
        Task<TeamMember?> GetByIdAsync(Guid id);
        Task<TeamMember> AddAsync(TeamMember entity);
        Task UpdateAsync(TeamMember entity);
        Task<bool> DeleteAsync(Guid id);

        Task<bool> TeamExistsAsync(Guid teamId);
        Task<bool> UserExistsAsync(Guid userId);
    }
}
