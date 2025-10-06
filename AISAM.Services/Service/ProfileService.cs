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

        public async Task<GenericResponse<ProfileResponseDto>> GetProfileByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var profile = await _profileRepository.GetByIdIncludingDeletedAsync(id, cancellationToken);
                if (profile == null)
                {
                    return GenericResponse<ProfileResponseDto>.CreateError("Không tìm thấy hồ sơ", HttpStatusCode.NotFound);
                }

                var profileDto = MapToDto(profile);
                var message = profile.IsDeleted ? "Lấy thông tin hồ sơ đã xóa thành công" : "Lấy thông tin hồ sơ thành công";
                return GenericResponse<ProfileResponseDto>.CreateSuccess(profileDto, message);
            }
            catch (Exception ex)
            {
                return GenericResponse<ProfileResponseDto>.CreateError($"Lỗi khi lấy thông tin hồ sơ: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<GenericResponse<IEnumerable<ProfileResponseDto>>> GetUserProfilesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return GenericResponse<IEnumerable<ProfileResponseDto>>.CreateError("Không tìm thấy người dùng", HttpStatusCode.NotFound);
                }

                var profiles = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
                var profileDtos = profiles.Select(MapToDto);

                return GenericResponse<IEnumerable<ProfileResponseDto>>.CreateSuccess(profileDtos, "Lấy danh sách hồ sơ thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<IEnumerable<ProfileResponseDto>>.CreateError($"Lỗi khi lấy danh sách hồ sơ người dùng: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<GenericResponse<IEnumerable<ProfileResponseDto>>> SearchUserProfilesAsync(Guid userId, string? searchTerm = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return GenericResponse<IEnumerable<ProfileResponseDto>>.CreateError("Không tìm thấy người dùng", HttpStatusCode.NotFound);
                }

                var profiles = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);

                // Apply search filter if provided
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    profiles = profiles.Where(p =>
                        (!string.IsNullOrEmpty(p.CompanyName) && p.CompanyName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(p.Bio) && p.Bio.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    );
                }

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

                // Validate business profile requirements
                // We allow users to create multiple profiles (including multiple of the same type),
                // but still enforce that business profiles have a company name.
                if (request.ProfileType == ProfileTypeEnum.Business && string.IsNullOrWhiteSpace(request.CompanyName))
                {
                    return GenericResponse<ProfileResponseDto>.CreateError("Tên công ty là bắt buộc đối với hồ sơ doanh nghiệp", HttpStatusCode.BadRequest);
                }

                // Validate personal profile should not have company name
                if (request.ProfileType == ProfileTypeEnum.Personal && !string.IsNullOrWhiteSpace(request.CompanyName))
                {
                    return GenericResponse<ProfileResponseDto>.CreateError("Hồ sơ cá nhân không được có tên công ty", HttpStatusCode.BadRequest);
                }

                // Handle avatar upload if provided
                string? avatarUrl = request.AvatarUrl;
                if (request.AvatarFile != null)
                {
                    try
                    {
                        // Validate file type
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                        var fileExtension = Path.GetExtension(request.AvatarFile.FileName).ToLowerInvariant();

                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            return GenericResponse<ProfileResponseDto>.CreateError("Loại tệp không hợp lệ. Chỉ cho phép tệp hình ảnh.", HttpStatusCode.BadRequest);
                        }

                        // Validate file size (5MB max)
                        const int maxFileSize = 5 * 1024 * 1024;
                        if (request.AvatarFile.Length > maxFileSize)
                        {
                            return GenericResponse<ProfileResponseDto>.CreateError("Kích thước tệp quá lớn. Tối đa 5MB.", HttpStatusCode.BadRequest);
                        }

                        // Generate avatar-specific filename
                        var uniqueFileName = $"avatars/{userId}_{Guid.NewGuid()}{fileExtension}";
                        var fileName = await _storageService.UploadFileAsync(request.AvatarFile.OpenReadStream(), uniqueFileName, request.AvatarFile.ContentType);
                        avatarUrl = _storageService.GetPublicUrl(fileName);
                    }
                    catch (Exception ex)
                    {
                        return GenericResponse<ProfileResponseDto>.CreateError($"Lỗi khi tải lên ảnh đại diện: {ex.Message}", HttpStatusCode.InternalServerError);
                    }
                }

                var profile = new Profile
                {
                    UserId = userId,
                    ProfileType = request.ProfileType,
                    CompanyName = request.CompanyName,
                    Bio = request.Bio,
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

                // Validate business profile and personal profile rules before updating
                if (request.ProfileType.HasValue && request.ProfileType == ProfileTypeEnum.Personal && !string.IsNullOrWhiteSpace(request.CompanyName))
                {
                    return GenericResponse<ProfileResponseDto>.CreateError("Hồ sơ cá nhân không được có tên công ty", HttpStatusCode.BadRequest);
                }

                // Update profile properties
                if (request.ProfileType.HasValue)
                {
                    profile.ProfileType = request.ProfileType.Value;

                    // Clear CompanyName if changing to Personal profile
                    if (profile.ProfileType == ProfileTypeEnum.Personal)
                    {
                        profile.CompanyName = null;
                    }
                }

                // Update CompanyName - LUÔN xử lý
                if (string.IsNullOrWhiteSpace(request.CompanyName))
                {
                    profile.CompanyName = null;
                }
                else
                {
                    var trimmedCompanyName = request.CompanyName.Trim();
                    // Double check: Don't allow CompanyName for Personal profiles
                    if (profile.ProfileType == ProfileTypeEnum.Personal)
                    {
                        return GenericResponse<ProfileResponseDto>.CreateError("Hồ sơ cá nhân không được có tên công ty", HttpStatusCode.BadRequest);
                    }
                    profile.CompanyName = trimmedCompanyName;
                }

                // Update Bio - LUÔN xử lý (kể cả khi null)
                // Nếu request.Bio là null hoặc empty -> set null, ngược lại set giá trị
                profile.Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim();

                // Validate business profile requirements after potential type change
                if (profile.ProfileType == ProfileTypeEnum.Business && string.IsNullOrWhiteSpace(profile.CompanyName))
                {
                    return GenericResponse<ProfileResponseDto>.CreateError("Tên công ty là bắt buộc đối với hồ sơ doanh nghiệp", HttpStatusCode.BadRequest);
                }

                // Handle avatar upload if provided, otherwise use URL from request
                if (request.AvatarFile != null)
                {
                    try
                    {
                        // Validate file type
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                        var fileExtension = Path.GetExtension(request.AvatarFile.FileName).ToLowerInvariant();

                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            return GenericResponse<ProfileResponseDto>.CreateError("Loại tệp không hợp lệ. Chỉ cho phép tệp hình ảnh.", HttpStatusCode.BadRequest);
                        }

                        // Validate file size (5MB max)
                        const int maxFileSize = 5 * 1024 * 1024;
                        if (request.AvatarFile.Length > maxFileSize)
                        {
                            return GenericResponse<ProfileResponseDto>.CreateError("Kích thước tệp quá lớn. Tối đa 5MB.", HttpStatusCode.BadRequest);
                        }

                        // Generate avatar-specific filename
                        var uniqueFileName = $"avatars/{profile.UserId}_{Guid.NewGuid()}{fileExtension}";
                        var fileName = await _storageService.UploadFileAsync(request.AvatarFile.OpenReadStream(), uniqueFileName, request.AvatarFile.ContentType);
                        profile.AvatarUrl = _storageService.GetPublicUrl(fileName);
                    }
                    catch (Exception ex)
                    {
                        return GenericResponse<ProfileResponseDto>.CreateError($"Lỗi khi tải lên ảnh đại diện: {ex.Message}", HttpStatusCode.InternalServerError);
                    }
                }
                else
                {
                    // Update AvatarUrl - LUÔN xử lý (kể cả khi null)
                    // Nếu request.AvatarUrl là null hoặc empty -> set null, ngược lại set giá trị
                    profile.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim();
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

                if (!profile.IsDeleted)
                {
                    return GenericResponse<bool>.CreateError("Hồ sơ chưa bị xóa, không thể khôi phục", HttpStatusCode.BadRequest);
                }

                profile.IsDeleted = false;
                profile.UpdatedAt = DateTime.UtcNow;

                await _profileRepository.UpdateAsync(profile, cancellationToken);
                return GenericResponse<bool>.CreateSuccess(true, "Khôi phục hồ sơ thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<bool>.CreateError($"Lỗi khi khôi phục hồ sơ: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<GenericResponse<bool>> PermanentDeleteProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
        {
            try
            {
                var profile = await _profileRepository.GetByIdIncludingDeletedAsync(profileId, cancellationToken);
                if (profile == null)
                {
                    return GenericResponse<bool>.CreateError("Không tìm thấy hồ sơ", HttpStatusCode.NotFound);
                }

                if (!profile.IsDeleted)
                {
                    return GenericResponse<bool>.CreateError("Hồ sơ phải được xóa mềm trước khi xóa vĩnh viễn", HttpStatusCode.BadRequest);
                }

                var deleted = await _profileRepository.PermanentDeleteAsync(profileId, cancellationToken);
                return GenericResponse<bool>.CreateSuccess(deleted, "Xóa vĩnh viễn hồ sơ thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<bool>.CreateError($"Lỗi khi xóa vĩnh viễn hồ sơ: {ex.Message}", HttpStatusCode.InternalServerError);
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