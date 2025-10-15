using AISAM.API.Utils;
using AISAM.Common;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/ads")]
    [Authorize]
    public class AdsController : ControllerBase
    {
        private readonly IAdService _service;
        private readonly IUserRepository _userRepository;

        public AdsController(IAdService service, IUserRepository userRepository)
        {
            _service = service;
            _userRepository = userRepository;
        }

        [HttpPost("campaigns")]
        public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignRequest request, CancellationToken ct)
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new UnauthorizedAccessException("User not found");
            var role = user.Role.ToString().ToLowerInvariant();
            var result = await _service.CreateCampaignAsync(userId, role, request, ct);
            return Ok(GenericResponse<CampaignResponse>.CreateSuccess(result));
        }

        [HttpPost("ad-sets")]
        public async Task<IActionResult> CreateAdSet([FromBody] CreateAdSetRequest request, CancellationToken ct)
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new UnauthorizedAccessException("User not found");
            var role = user.Role.ToString().ToLowerInvariant();
            var result = await _service.CreateAdSetAsync(userId, role, request, ct);
            return Ok(GenericResponse<AdSetResponse>.CreateSuccess(result));
        }

        [HttpPost("creatives")]
        public async Task<IActionResult> CreateCreative([FromBody] CreateCreativeRequest request, CancellationToken ct)
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new UnauthorizedAccessException("User not found");
            var role = user.Role.ToString().ToLowerInvariant();
            var result = await _service.CreateCreativeAsync(userId, role, request, ct);
            return Ok(GenericResponse<CreativeResponse>.CreateSuccess(result));
        }

        [HttpPost]
        public async Task<IActionResult> PublishAd([FromBody] PublishAdRequest request, CancellationToken ct)
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new UnauthorizedAccessException("User not found");
            var role = user.Role.ToString().ToLowerInvariant();
            var result = await _service.PublishAdAsync(userId, role, request, ct);
            return Ok(GenericResponse<AdResponse>.CreateSuccess(result));
        }

        [HttpGet("campaigns")]
        public async Task<IActionResult> GetCampaigns([FromQuery] Guid? brandId, CancellationToken ct)
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new UnauthorizedAccessException("User not found");
            var role = user.Role.ToString().ToLowerInvariant();
            var result = await _service.GetCampaignsAsync(userId, brandId, role, ct);
            return Ok(GenericResponse<List<CampaignResponse>>.CreateSuccess(result));
        }

        [HttpGet]
        public async Task<IActionResult> GetAds([FromQuery] Guid? brandId, [FromQuery] Guid? campaignId, CancellationToken ct)
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new UnauthorizedAccessException("User not found");
            var role = user.Role.ToString().ToLowerInvariant();
            var result = await _service.GetAdsAsync(userId, brandId, campaignId, role, ct);
            return Ok(GenericResponse<List<AdResponse>>.CreateSuccess(result));
        }

        [HttpPut("campaigns/{id}")]
        public async Task<IActionResult> UpdateCampaign([FromRoute] Guid id, [FromBody] UpdateCampaignRequest request, CancellationToken ct)
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new UnauthorizedAccessException("User not found");
            var role = user.Role.ToString().ToLowerInvariant();
            var result = await _service.UpdateCampaignAsync(userId, role, id, request, ct);
            return Ok(GenericResponse<CampaignResponse>.CreateSuccess(result));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAd([FromRoute] Guid id, [FromBody] UpdateAdRequest request, CancellationToken ct)
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new UnauthorizedAccessException("User not found");
            var role = user.Role.ToString().ToLowerInvariant();
            var result = await _service.UpdateAdAsync(userId, role, id, request, ct);
            return Ok(GenericResponse<AdResponse>.CreateSuccess(result));
        }

        [HttpDelete("campaigns/{id}")]
        public async Task<IActionResult> DeleteCampaign([FromRoute] Guid id, CancellationToken ct)
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new UnauthorizedAccessException("User not found");
            var role = user.Role.ToString().ToLowerInvariant();
            await _service.DeleteCampaignAsync(userId, role, id, ct);
            return Ok(GenericResponse.CreateSuccess());
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAd([FromRoute] Guid id, CancellationToken ct)
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new UnauthorizedAccessException("User not found");
            var role = user.Role.ToString().ToLowerInvariant();
            await _service.DeleteAdAsync(userId, role, id, ct);
            return Ok(GenericResponse.CreateSuccess());
        }

        [HttpPost("{id}/pull-reports")]
        public async Task<IActionResult> PullReports([FromRoute] Guid id, CancellationToken ct)
        {
            var userId = UserClaimsHelper.GetUserIdOrThrow(User);
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new UnauthorizedAccessException("User not found");
            var role = user.Role.ToString().ToLowerInvariant();
            await _service.PullReportsAsync(userId, role, id, ct);
            return Ok(GenericResponse.CreateSuccess());
        }
    }
}


