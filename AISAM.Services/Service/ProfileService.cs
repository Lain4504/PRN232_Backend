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
        private readonly SupabaseStorageService _storageService;

        public ProfileService(IProfileRepository profileRepository, IUserRepository userRepository, SupabaseStorageService storageService)
        {
            _profileRepository = profileRepository;
            _userRepository = userRepository;
            _storageService = storageService;
        }

        public async Task<GenericResponse<ProfileResponseDto>> GetProfileByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var profile = await _profileRepository.GetByIdIncludingDeletedAsync(id, cancellationToken);
                if (profile == null)
                {
                    return GenericResponse<ProfileResponseDto>.CreateError("Không tìm thấy hồ sơ", HttpStatusCode.NotFound);
                }

                // Kiểm tra nếu profile đã bị xóa mềm
                if (profile.IsDeleted)
                {
                    return GenericResponse<ProfileResponseDto>.CreateError("Hồ sơ đã bị xóa", HttpStatusCode.Gone);
                }

                var profileDto = MapToDto(profile);
                return GenericResponse<ProfileResponseDto>.CreateSuccess(profileDto, "Lấy thông tin hồ sơ thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<ProfileResponseDto>.CreateError($"Lỗi khi lấy thông tin hồ sơ: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<GenericResponse<IEnumerable<ProfileResponseDto>>> SearchUserProfilesAsync(Guid userId, string? searchTerm = null, bool? isDeleted = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return GenericResponse<IEnumerable<ProfileResponseDto>>.CreateError("Không tìm thấy người dùng", HttpStatusCode.NotFound);
                }

                // Use repository method to search profiles
                var profiles = await _profileRepository.SearchUserProfilesAsync(userId, searchTerm, isDeleted, cancellationToken);

                var profileDtos = profiles.Select(MapToDto);
                return GenericResponse<IEnumerable<ProfileResponseDto>>.CreateSuccess(profileDtos, "Tìm kiếm hồ sơ thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<IEnumerable<ProfileResponseDto>>.CreateError($"Lỗi khi tìm kiếm hồ sơ người dùng: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<GenericResponse<ProfileResponseDto>> CreateProfileAsync(Guid userId, CreateProfileRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return GenericResponse<ProfileResponseDto>.CreateError("Không tìm thấy người dùng", HttpStatusCode.NotFound);
                }

                // Handle avatar upload if provided
                string? avatarUrl = request.AvatarUrl;
                if (request.AvatarFile != null)
                {
                    try
                    {
                        // Use UploadFileAsync which handles validation and stream extraction automatically
                        var fileName = await _storageService.UploadFileAsync(request.AvatarFile, DefaultBucketEnum.Avatar);
                        avatarUrl = _storageService.GetPublicUrl(fileName, DefaultBucketEnum.Avatar);
                    }
                    catch (InvalidOperationException ex)
                    {
                        // Handle validation errors from SupabaseStorageService
                        return GenericResponse<ProfileResponseDto>.CreateError(ex.Message, HttpStatusCode.BadRequest);
                    }
                    catch (Exception ex)
                    {
                        return GenericResponse<ProfileResponseDto>.CreateError($"Lỗi khi tải lên ảnh đại diện: {ex.Message}", HttpStatusCode.InternalServerError);
                    }
                }

                var profile = new Profile
                {
                    UserId = userId,
                    Name = request.Name.Trim(),
                    ProfileType = request.ProfileType,
                    CompanyName = string.IsNullOrWhiteSpace(request.CompanyName) ? null : request.CompanyName.Trim(),
                    Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim(),
                    AvatarUrl = avatarUrl,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdProfile = await _profileRepository.CreateAsync(profile, cancellationToken);
                var profileDto = MapToDto(createdProfile);

                return GenericResponse<ProfileResponseDto>.CreateSuccess(profileDto, "Tạo hồ sơ thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<ProfileResponseDto>.CreateError($"Lỗi khi tạo hồ sơ: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<GenericResponse<ProfileResponseDto>> UpdateProfileAsync(Guid profileId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var profile = await _profileRepository.GetByIdAsync(profileId, cancellationToken);
                if (profile == null)
                {
                    return GenericResponse<ProfileResponseDto>.CreateError("Không tìm thấy hồ sơ", HttpStatusCode.NotFound);
                }

                // Update Name if provided
                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    profile.Name = request.Name.Trim();
                }

                // Update profile properties
                if (request.ProfileType.HasValue)
                {
                    profile.ProfileType = request.ProfileType.Value;
                }

                // Update CompanyName - normalize and apply
                profile.CompanyName = string.IsNullOrWhiteSpace(request.CompanyName)
                    ? null
                    : request.CompanyName.Trim();

                // Update Bio - normalize and apply  
                profile.Bio = string.IsNullOrWhiteSpace(request.Bio)
                    ? null
                    : request.Bio.Trim();

                // Handle avatar upload if provided, otherwise use URL from request
                if (request.AvatarFile != null)
                {
                    try
                    {
                        // Use UploadFileAsync which handles validation and stream extraction automatically
                        var fileName = await _storageService.UploadFileAsync(request.AvatarFile, DefaultBucketEnum.Avatar);
                        profile.AvatarUrl = _storageService.GetPublicUrl(fileName, DefaultBucketEnum.Avatar);
                    }
                    catch (InvalidOperationException ex)
                    {
                        // Handle validation errors from SupabaseStorageService
                        return GenericResponse<ProfileResponseDto>.CreateError(ex.Message, HttpStatusCode.BadRequest);
                    }
                    catch (Exception ex)
                    {
                        return GenericResponse<ProfileResponseDto>.CreateError($"Lỗi khi tải lên ảnh đại diện: {ex.Message}", HttpStatusCode.InternalServerError);
                    }
                }
                else
                {
                    // Update AvatarUrl - normalize and apply
                    profile.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl)
                        ? null
                        : request.AvatarUrl.Trim();
                }

                profile.UpdatedAt = DateTime.UtcNow;

                var updatedProfile = await _profileRepository.UpdateAsync(profile, cancellationToken);
                var profileDto = MapToDto(updatedProfile);

                return GenericResponse<ProfileResponseDto>.CreateSuccess(profileDto, "Cập nhật hồ sơ thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<ProfileResponseDto>.CreateError($"Lỗi khi cập nhật hồ sơ: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<GenericResponse<bool>> DeleteProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
        {
            try
            {
                var profileExists = await _profileRepository.ExistsAsync(profileId, cancellationToken);
                if (!profileExists)
                {
                    return GenericResponse<bool>.CreateError("Không tìm thấy hồ sơ", HttpStatusCode.NotFound);
                }

                var deleted = await _profileRepository.DeleteAsync(profileId, cancellationToken);
                return GenericResponse<bool>.CreateSuccess(deleted, "Xóa hồ sơ thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<bool>.CreateError($"Lỗi khi xóa hồ sơ: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<GenericResponse<bool>> RestoreProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
        {
            try
            {
                var profile = await _profileRepository.GetByIdIncludingDeletedAsync(profileId, cancellationToken);
                if (profile == null)
                {
                    return GenericResponse<bool>.CreateError("Không tìm thấy hồ sơ", HttpStatusCode.NotFound);
                }

                if (profile.Status != ProfileStatusEnum.Cancelled)
                {
                    return GenericResponse<bool>.CreateError("Hồ sơ chưa bị xóa, không thể khôi phục", HttpStatusCode.BadRequest);
                }

                profile.Status = ProfileStatusEnum.Pending; // Restore to pending status
                profile.UpdatedAt = DateTime.UtcNow;

                await _profileRepository.UpdateAsync(profile, cancellationToken);
                return GenericResponse<bool>.CreateSuccess(true, "Khôi phục hồ sơ thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<bool>.CreateError($"Lỗi khi khôi phục hồ sơ: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        private static ProfileResponseDto MapToDto(Profile profile)
        {
            return new ProfileResponseDto
            {
                Id = profile.Id,
                UserId = profile.UserId,
                Name = profile.Name,
                ProfileType = profile.ProfileType,
                SubscriptionId = profile.SubscriptionId,
                CompanyName = profile.CompanyName,
                Bio = profile.Bio,
                AvatarUrl = profile.AvatarUrl,
                Status = profile.Status,
                CreatedAt = profile.CreatedAt,
                UpdatedAt = profile.UpdatedAt
            };
        }
    }
}