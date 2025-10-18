using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Common;
using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;

namespace AISAM.Services.IServices
{
    public interface ITeamService
    {
        Task<GenericResponse<TeamResponse>> CreateTeamAsync(CreateTeamRequest request, Guid vendorId);
        Task<GenericResponse<TeamResponse>> GetTeamByIdAsync(Guid id, Guid userId);
        Task<GenericResponse<IEnumerable<TeamResponse>>> GetTeamsByVendorAsync(Guid vendorId, Guid userId);
        Task<GenericResponse<TeamResponse>> UpdateTeamAsync(Guid id, CreateTeamRequest request, Guid userId);
        Task<GenericResponse<bool>> DeleteTeamAsync(Guid id, Guid userId);
        Task<GenericResponse<bool>> UpdateTeamStatusAsync(Guid id, UpdateTeamStatusRequest request, Guid userId);
        Task<GenericResponse<bool>> RestoreTeamAsync(Guid id, Guid userId);
        Task<GenericResponse<bool>> AssignBrandToTeamAsync(Guid id, AssignBrandToTeamRequest request, Guid userId);
        Task<GenericResponse<bool>> UnassignBrandFromTeamAsync(Guid teamId, UnassignBrandFromTeamRequest request, Guid userId);
        Task<GenericResponse<IEnumerable<TeamMemberResponseDto>>> GetTeamMembersAsync(Guid teamId, Guid userId);
    }
}