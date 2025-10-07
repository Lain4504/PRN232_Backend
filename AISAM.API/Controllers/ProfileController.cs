using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AISAM.Common;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Services.IServices;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/profiles")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(IProfileService profileService, ILogger<ProfileController> logger)
        {
            _profileService = profileService;
            _logger = logger;
        }

        /// <summary>
        /// Get all profiles for a specific user with optional search and deletion status filter
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<GenericResponse<IEnumerable<ProfileResponseDto>>>> GetUserProfiles(
            Guid userId,
            [FromQuery] string? search = null,
            [FromQuery] bool? isDeleted = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _profileService.SearchUserProfilesAsync(userId, search, isDeleted, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách hồ sơ cho user: {UserId}", userId);
                return StatusCode(500, GenericResponse<IEnumerable<ProfileResponseDto>>.CreateError("Lỗi hệ thống"));
            }
        }

        /// <summary>
        /// Get a specific profile by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<GenericResponse<ProfileResponseDto>>> GetProfile(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _profileService.GetProfileByIdAsync(id, cancellationToken);
                if (!result.Success)
                {
                    return NotFound(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin hồ sơ với ID: {id}", id);
                return StatusCode(500, GenericResponse<ProfileResponseDto>.CreateError("Lỗi hệ thống"));
            }
        }

        /// <summary>
        /// Create a new profile for a specific user
        /// </summary>
        [HttpPost("user/{userId}")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<GenericResponse<ProfileResponseDto>>> CreateProfile(Guid userId, [FromForm] CreateProfileRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _profileService.CreateProfileAsync(userId, request, cancellationToken);
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return CreatedAtAction(nameof(GetProfile), new { id = result.Data!.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo hồ sơ cho user: {UserId}", userId);
                return StatusCode(500, GenericResponse<ProfileResponseDto>.CreateError("Lỗi hệ thống"));
            }
        }

        /// <summary>
        /// Update an existing profile
        /// </summary>
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<GenericResponse<ProfileResponseDto>>> UpdateProfile(Guid id, [FromForm] UpdateProfileRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _profileService.UpdateProfileAsync(id, request, cancellationToken);
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật hồ sơ với ID: {id}", id);
                return StatusCode(500, GenericResponse<ProfileResponseDto>.CreateError("Lỗi hệ thống"));
            }
        }

        /// <summary>
        /// Delete a profile (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<GenericResponse<bool>>> DeleteProfile(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _profileService.DeleteProfileAsync(id, cancellationToken);
                if (!result.Success)
                {
                    return NotFound(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa hồ sơ với ID: {id}", id);
                return StatusCode(500, GenericResponse<bool>.CreateError("Lỗi hệ thống"));
            }
        }

        /// <summary>
        /// Restore a soft deleted profile
        /// </summary>
        [HttpPatch("{id}/restore")]
        public async Task<ActionResult<GenericResponse<bool>>> RestoreProfile(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _profileService.RestoreProfileAsync(id, cancellationToken);
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi khôi phục hồ sơ với ID: {id}", id);
                return StatusCode(500, GenericResponse<bool>.CreateError("Lỗi hệ thống"));
            }
        }
    }
}