using AISAM.Common;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Data.Enumeration;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using System.Net;

namespace AISAM.Services.Service
{
    public class ProfileService : IProfileService
    {
        private readonly IProfileRepository _profileRepository;
        private readonly IUserRepository _userRepository;

        public ProfileService(IProfileRepository profileRepository, IUserRepository userRepository)
        {
            _profileRepository = profileRepository;
            _userRepository = userRepository;
        }

        public async Task<GenericResponse<ProfileResponseDto>> GetProfileByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var profile = await _profileRepository.GetByIdAsync(id, cancellationToken);
                if (profile == null)
                {
                    return GenericResponse<ProfileResponseDto>.CreateError("Profile not found", HttpStatusCode.NotFound);
                }

                var profileDto = MapToDto(profile);
                return GenericResponse<ProfileResponseDto>.CreateSuccess(profileDto, "Profile retrieved successfully");
            }
            catch (Exception ex)
            {
                return GenericResponse<ProfileResponseDto>.CreateError($"Error retrieving profile: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<GenericResponse<IEnumerable<ProfileResponseDto>>> GetUserProfilesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    return GenericResponse<IEnumerable<ProfileResponseDto>>.CreateError("User not found", HttpStatusCode.NotFound);
                }

                var profiles = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
                var profileDtos = profiles.Select(MapToDto);

                return GenericResponse<IEnumerable<ProfileResponseDto>>.CreateSuccess(profileDtos, "Profiles retrieved successfully");
            }
            catch (Exception ex)
            {
                return GenericResponse<IEnumerable<ProfileResponseDto>>.CreateError($"Error retrieving user profiles: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<GenericResponse<ProfileResponseDto>> GetUserProfileByTypeAsync(Guid userId, ProfileTypeEnum profileType, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    return GenericResponse<ProfileResponseDto>.CreateError("User not found", HttpStatusCode.NotFound);
                }

                var profile = await _profileRepository.GetByUserIdAndTypeAsync(userId, profileType, cancellationToken);
                if (profile == null)
                {
                    return GenericResponse<ProfileResponseDto>.CreateError($"Profile of type {profileType} not found for user", HttpStatusCode.NotFound);
                }

                var profileDto = MapToDto(profile);
                return GenericResponse<ProfileResponseDto>.CreateSuccess(profileDto, "Profile retrieved successfully");
            }
            catch (Exception ex)
            {
                return GenericResponse<ProfileResponseDto>.CreateError($"Error retrieving user profile: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<GenericResponse<IEnumerable<ProfileResponseDto>>> GetUserProfilesByTypeAsync(Guid userId, ProfileTypeEnum profileType, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    return GenericResponse<IEnumerable<ProfileResponseDto>>.CreateError("User not found", HttpStatusCode.NotFound);
                }

                var profiles = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
                var filtered = profiles.Where(p => p.ProfileType == profileType).Select(MapToDto);
                return GenericResponse<IEnumerable<ProfileResponseDto>>.CreateSuccess(filtered, "Profiles retrieved successfully");
            }
            catch (Exception ex)
            {
                return GenericResponse<IEnumerable<ProfileResponseDto>>.CreateError($"Error retrieving user profiles: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<GenericResponse<ProfileResponseDto>> CreateProfileAsync(Guid userId, CreateProfileRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    // Create user automatically for testing
                    user = new User
                    {
                        Id = userId,
                        Email = "test@example.com",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    user = await _userRepository.CreateAsync(user, cancellationToken);
                }

                // Validate business profile requirements
                // We allow users to create multiple profiles (including multiple of the same type),
                // but still enforce that business profiles have a company name.
                if (request.ProfileType == ProfileTypeEnum.Business && string.IsNullOrWhiteSpace(request.CompanyName))
                {
                    return GenericResponse<ProfileResponseDto>.CreateError("Company name is required for business profiles", HttpStatusCode.BadRequest);
                }

                var profile = new Profile
                {
                    UserId = userId,
                    FullName = request.FullName,
                    ProfileType = request.ProfileType,
                    CompanyName = request.CompanyName,
                    Bio = request.Bio,
                    AvatarUrl = request.AvatarUrl,
                    DateOfBirth = request.DateOfBirth,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdProfile = await _profileRepository.CreateAsync(profile, cancellationToken);
                var profileDto = MapToDto(createdProfile);

                return GenericResponse<ProfileResponseDto>.CreateSuccess(profileDto, "Profile created successfully");
            }
            catch (Exception ex)
            {
                return GenericResponse<ProfileResponseDto>.CreateError($"Error creating profile: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<GenericResponse<ProfileResponseDto>> UpdateProfileAsync(Guid profileId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var profile = await _profileRepository.GetByIdAsync(profileId, cancellationToken);
                if (profile == null)
                {
                    return GenericResponse<ProfileResponseDto>.CreateError("Profile not found", HttpStatusCode.NotFound);
                }

                // Update profile properties
                if (!string.IsNullOrEmpty(request.CompanyName))
                {
                    profile.CompanyName = request.CompanyName;
                }

                if (!string.IsNullOrEmpty(request.Bio))
                {
                    profile.Bio = request.Bio;
                }

                if (!string.IsNullOrEmpty(request.AvatarUrl))
                {
                    profile.AvatarUrl = request.AvatarUrl;
                }

                var updatedProfile = await _profileRepository.UpdateAsync(profile, cancellationToken);
                var profileDto = MapToDto(updatedProfile);

                return GenericResponse<ProfileResponseDto>.CreateSuccess(profileDto, "Profile updated successfully");
            }
            catch (Exception ex)
            {
                return GenericResponse<ProfileResponseDto>.CreateError($"Error updating profile: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<GenericResponse<bool>> DeleteProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
        {
            try
            {
                var profileExists = await _profileRepository.ExistsAsync(profileId, cancellationToken);
                if (!profileExists)
                {
                    return GenericResponse<bool>.CreateError("Profile not found", HttpStatusCode.NotFound);
                }

                var deleted = await _profileRepository.DeleteAsync(profileId, cancellationToken);
                return GenericResponse<bool>.CreateSuccess(deleted, "Profile deleted successfully");
            }
            catch (Exception ex)
            {
                return GenericResponse<bool>.CreateError($"Error deleting profile: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        private static ProfileResponseDto MapToDto(Profile profile)
        {
            return new ProfileResponseDto
            {
                Id = profile.Id,
                UserId = profile.UserId,
                ProfileType = profile.ProfileType,
                CompanyName = profile.CompanyName,
                Bio = profile.Bio,
                AvatarUrl = profile.AvatarUrl,
                CreatedAt = profile.CreatedAt,
                UpdatedAt = profile.UpdatedAt
            };
        }
    }
}