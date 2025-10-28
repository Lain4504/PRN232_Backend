using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Common;
using AISAM.Common.Dtos;

namespace AISAM.Services.IServices
{
    public interface ITeamService
    {
        Task<GenericResponse<TeamResponse>> CreateTeamAsync(CreateTeamRequest request, Guid profileId, Guid userId);
        Task<GenericResponse<TeamResponse>> GetTeamByIdAsync(Guid id, Guid profileId, Guid userId);
        Task<GenericResponse<IEnumerable<TeamResponse>>> GetTeamsByProfileAsync(Guid profileId, Guid userId);
        Task<GenericResponse<TeamResponse>> UpdateTeamAsync(Guid id, UpdateTeamRequest request, Guid userId);
        Task<GenericResponse<bool>> DeleteTeamAsync(Guid id, Guid userId);
        Task<GenericResponse<bool>> RestoreTeamAsync(Guid id, Guid userId);
        Task<GenericResponse<bool>> AssignBrandToTeamAsync(Guid id, AssignBrandToTeamRequest request, Guid userId);
        Task<GenericResponse<IEnumerable<TeamMemberResponseDto>>> GetTeamMembersAsync(Guid teamId, Guid profileId, Guid userId);
        Task<GenericResponse<IEnumerable<string>>> GetMyPermissionsAsync(Guid teamId, Guid userId);
        Task<GenericResponse<bool>> UnassignBrandFromTeamAsync(Guid teamId, UnassignBrandFromTeamRequest request, Guid userId);
        Task<GenericResponse<IEnumerable<TeamResponse>>> GetUserTeamsAsync(Guid userId);
    }
}