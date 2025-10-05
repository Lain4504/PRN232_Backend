using AISAM.Common;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Data.Enumeration;
using AISAM.Data.Model;

namespace AISAM.Services.IServices
{
    public interface IProfileService
    {
        Task<GenericResponse<ProfileResponseDto>> GetProfileByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<GenericResponse<IEnumerable<ProfileResponseDto>>> GetUserProfilesAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<GenericResponse<ProfileResponseDto>> GetUserProfileByTypeAsync(Guid userId, ProfileTypeEnum profileType, CancellationToken cancellationToken = default);
        Task<GenericResponse<IEnumerable<ProfileResponseDto>>> GetUserProfilesByTypeAsync(Guid userId, ProfileTypeEnum profileType, CancellationToken cancellationToken = default);
        Task<GenericResponse<ProfileResponseDto>> CreateProfileAsync(Guid userId, CreateProfileRequest request, CancellationToken cancellationToken = default);
        Task<GenericResponse<ProfileResponseDto>> UpdateProfileAsync(Guid profileId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
        Task<GenericResponse<bool>> DeleteProfileAsync(Guid profileId, CancellationToken cancellationToken = default);
    }
}