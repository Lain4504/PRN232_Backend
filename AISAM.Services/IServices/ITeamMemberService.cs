using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;

namespace AISAM.Services.IServices
{
    public interface ITeamMemberService
    {
        Task<PagedResult<TeamMemberResponseDto>> GetPagedAsync(PaginationRequest request);
        Task<TeamMemberResponseDto?> GetByIdAsync(Guid id, Guid userId);
        Task<TeamMemberResponseDto> CreateAsync(TeamMemberCreateRequest request, Guid userId);
        Task<TeamMemberResponseDto?> UpdateAsync(Guid id, TeamMemberUpdateRequest request, Guid userId);
        Task<bool> DeleteAsync(Guid id, Guid userId);
    }
}
