using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;

namespace AISAM.Services.IServices
{
    public interface ITeamMemberService
    {
        Task<PagedResult<TeamMemberResponseDto>> GetPagedAsync(PaginationRequest request);
        Task<TeamMemberResponseDto?> GetByIdAsync(Guid id);
        Task<TeamMemberResponseDto> CreateAsync(TeamMemberCreateRequest request);
        Task<TeamMemberResponseDto?> UpdateAsync(Guid id, TeamMemberUpdateRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}
