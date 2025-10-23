using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Common;
using AISAM.Common.Dtos;

namespace AISAM.Services.IServices
{
    public interface ITeamService
    {
        Task<GenericResponse<TeamResponse>> CreateTeamAsync(CreateTeamRequest request, Guid profileId);
        Task<GenericResponse<TeamResponse>> GetTeamByIdAsync(Guid id, Guid profileId);
        Task<GenericResponse<IEnumerable<TeamResponse>>> GetTeamsByProfileAsync(Guid profileId, Guid authenticatedProfileId);
        Task<GenericResponse<TeamResponse>> UpdateTeamAsync(Guid id, CreateTeamRequest request, Guid profileId);
        Task<GenericResponse<bool>> DeleteTeamAsync(Guid id, Guid profileId);
        Task<GenericResponse<bool>> UpdateTeamStatusAsync(Guid id, UpdateTeamStatusRequest request, Guid profileId);
        Task<GenericResponse<bool>> RestoreTeamAsync(Guid id, Guid profileId);
        Task<GenericResponse<bool>> AssignBrandToTeamAsync(Guid id, AssignBrandToTeamRequest request, Guid profileId);
        Task<GenericResponse<IEnumerable<TeamMemberResponseDto>>> GetTeamMembersAsync(Guid teamId, Guid profileId);
        Task<GenericResponse<IEnumerable<TeamResponse>>> GetTeamsByVendorAsync(Guid vendorId, Guid userId);
        Task<GenericResponse<bool>> UnassignBrandFromTeamAsync(Guid teamId, UnassignBrandFromTeamRequest request, Guid userId);
    }
}