using AISAM.Common.DTOs.Request;
using AISAM.Common.DTOs.Response;
using AISAM.Common;

namespace AISAM.Services.IServices
{
    public interface ITeamService
    {
        Task<GenericResponse<TeamResponse>> CreateTeamAsync(CreateTeamRequest request, Guid vendorId);
        Task<GenericResponse<TeamResponse>> GetTeamByIdAsync(Guid id, Guid userId);
        Task<GenericResponse<IEnumerable<TeamResponse>>> GetTeamsByVendorAsync(Guid vendorId, Guid userId);
        Task<GenericResponse<TeamResponse>> UpdateTeamAsync(Guid id, CreateTeamRequest request, Guid userId);
        Task<GenericResponse<bool>> DeleteTeamAsync(Guid id, Guid userId);
    }
}