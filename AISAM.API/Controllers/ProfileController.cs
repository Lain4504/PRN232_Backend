using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AISAM.Common;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Data.Enumeration;
using AISAM.Services.IServices;
using System.Security.Claims;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/profiles")]
    // [Authorize] // Temporarily disabled for testing
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
        /// Get all profiles for the current user
        /// </summary>
        [HttpGet("my-profiles")]
        public async Task<ActionResult<GenericResponse<IEnumerable<ProfileResponseDto>>>> GetMyProfiles()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(GenericResponse<IEnumerable<ProfileResponseDto>>.CreateError("Invalid token"));
                }

                var result = await _profileService.GetUserProfilesAsync(userId.Value);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profiles");
                return StatusCode(500, GenericResponse<IEnumerable<ProfileResponseDto>>.CreateError("Internal server error"));
            }
        }

        /// <summary>
        /// Get a specific profile by ID
        /// </summary>
        [HttpGet("{profileId}")]
        public async Task<ActionResult<GenericResponse<ProfileResponseDto>>> GetProfile(Guid profileId)
        {
            try
            {
                var result = await _profileService.GetProfileByIdAsync(profileId);
                if (!result.Success)
                {
                    return NotFound(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profile with ID: {ProfileId}", profileId);
                return StatusCode(500, GenericResponse<ProfileResponseDto>.CreateError("Internal server error"));
            }
        }

        /// <summary>
        /// Get user's profile by type (Personal or Business)
        /// </summary>
        [HttpGet("by-type/{profileType}")]
        public async Task<ActionResult<GenericResponse<IEnumerable<ProfileResponseDto>>>> GetProfileByType(ProfileTypeEnum profileType)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(GenericResponse<IEnumerable<ProfileResponseDto>>.CreateError("Invalid token"));
                }

                var result = await _profileService.GetUserProfilesByTypeAsync(userId.Value, profileType);
                if (!result.Success)
                {
                    return NotFound(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profile by type: {ProfileType} for user: {UserId}", profileType, GetCurrentUserId());
                return StatusCode(500, GenericResponse<IEnumerable<ProfileResponseDto>>.CreateError("Internal server error"));
            }
        }

        /// <summary>
        /// Create a new profile for the current user
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<GenericResponse<ProfileResponseDto>>> CreateProfile([FromBody] CreateProfileRequest request)
        {
            try
            {
                // Hardcoded User ID for testing (same as from token: 6bdf746e-61da-4868-9a3e-3fe327046b8d)
                var userId = Guid.Parse("6bdf746e-61da-4868-9a3e-3fe327046b8d");

                var result = await _profileService.CreateProfileAsync(userId, request);
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return CreatedAtAction(nameof(GetProfile), new { profileId = result.Data!.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating profile for user: {UserId}", "6bdf746e-61da-4868-9a3e-3fe327046b8d");
                return StatusCode(500, GenericResponse<ProfileResponseDto>.CreateError("Internal server error"));
            }
        }

        /// <summary>
        /// Update an existing profile
        /// </summary>
        [HttpPut("{profileId}")]
        public async Task<ActionResult<GenericResponse<ProfileResponseDto>>> UpdateProfile(Guid profileId, [FromBody] UpdateProfileRequest request)
        {
            try
            {
                var result = await _profileService.UpdateProfileAsync(profileId, request);
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile with ID: {ProfileId}", profileId);
                return StatusCode(500, GenericResponse<ProfileResponseDto>.CreateError("Internal server error"));
            }
        }

        /// <summary>
        /// Delete a profile (soft delete)
        /// </summary>
        [HttpDelete("{profileId}")]
        public async Task<ActionResult<GenericResponse<bool>>> DeleteProfile(Guid profileId)
        {
            try
            {
                var result = await _profileService.DeleteProfileAsync(profileId);
                if (!result.Success)
                {
                    return NotFound(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting profile with ID: {ProfileId}", profileId);
                return StatusCode(500, GenericResponse<bool>.CreateError("Internal server error"));
            }
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                return userId;
            }
            return null;
        }
    }
}