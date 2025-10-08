using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Common.Models;

namespace AISAM.Services.IServices
{
    public interface ITeamService
    {
        Task<TeamResponseDto?> GetByIdAsync(Guid id, Guid currentUserId);
        Task<PagedResult<TeamResponseDto>> GetPagedAsync(Guid currentUserId, PaginationRequest request);
        Task<TeamResponseDto> CreateAsync(Guid currentUserId, CreateTeamRequest request);
        Task<TeamResponseDto?> UpdateAsync(Guid id, Guid currentUserId, UpdateTeamRequest request);
        Task<bool> SoftDeleteAsync(Guid id, Guid currentUserId);
    }
}
