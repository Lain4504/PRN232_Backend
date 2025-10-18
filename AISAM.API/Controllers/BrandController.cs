using Microsoft.AspNetCore.Mvc;
using AISAM.Common;
using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using AISAM.API.Utils;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/brands")]
    public class BrandController : ControllerBase
    {
        private readonly IBrandService _brandService;
        private readonly ILogger<BrandController> _logger;

        public BrandController(IBrandService service, ILogger<BrandController> logger)
        {
            _brandService = service;
            _logger = logger;
        }

        /// <summary>
        /// Lấy brand theo Id
        /// GET api/brands/{id}
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<BrandResponseDto>>> GetById(Guid id)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var brand = await _brandService.GetByIdAsync(id, userId);
                if (brand == null)
                    return NotFound(GenericResponse<BrandResponseDto>.CreateError("Không tìm thấy brand"));

                return Ok(GenericResponse<BrandResponseDto>.CreateSuccess(brand, "Lấy brand thành công"));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to brand {BrandId}", id);
                return StatusCode(403, GenericResponse<BrandResponseDto>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting brand {BrandId}", id);
                return StatusCode(500, GenericResponse<BrandResponseDto>.CreateError("Đã xảy ra lỗi khi lấy brand"));
            }
        }

        /// <summary>
        /// Lấy danh sách brand của user hiện tại phân trang
        /// GET api/brands?page=1&pageSize=10
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<GenericResponse<PagedResult<BrandResponseDto>>>> GetBrands(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDescending = true)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);

                var paginationRequest = new PaginationRequest
                {
                    Page = page,
                    PageSize = pageSize,
                    SearchTerm = searchTerm,
                    SortBy = sortBy,
                    SortDescending = sortDescending
                };

                var result = await _brandService.GetPagedByUserIdAsync(userId, paginationRequest);
                return Ok(GenericResponse<PagedResult<BrandResponseDto>>.CreateSuccess(result, "Lấy danh sách brand thành công"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(GenericResponse<PagedResult<BrandResponseDto>>.CreateError("Token không hợp lệ"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated brands");
                return StatusCode(500, GenericResponse<PagedResult<BrandResponseDto>>.CreateError("Đã xảy ra lỗi khi lấy danh sách brand"));
            }
        }
        [HttpGet("team")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<PagedResult<BrandResponseDto>>>> GetBrandsByTeamMembership(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDescending = true)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);

                var paginationRequest = new PaginationRequest
                {
                    Page = page,
                    PageSize = pageSize,
                    SearchTerm = searchTerm,
                    SortBy = sortBy,
                    SortDescending = sortDescending
                };

                var result = await _brandService.GetPagedBrandsByTeamMembershipAsync(userId, paginationRequest);
                return Ok(GenericResponse<PagedResult<BrandResponseDto>>.CreateSuccess(result, "Lấy danh sách brand thành công"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(GenericResponse<PagedResult<BrandResponseDto>>.CreateError("Token không hợp lệ"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated brands");
                return StatusCode(500, GenericResponse<PagedResult<BrandResponseDto>>.CreateError("Đã xảy ra lỗi khi lấy danh sách brand"));
            }
        }

        /// <summary>
        /// Lấy danh sách brands của một team cụ thể
        /// GET api/brands/team/{teamId}
        /// </summary>
        [HttpGet("team/{teamId}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<IEnumerable<BrandResponseDto>>>> GetBrandsByTeamId(Guid teamId)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);
                var result = await _brandService.GetBrandsByTeamIdAsync(teamId, userId);
                return Ok(GenericResponse<IEnumerable<BrandResponseDto>>.CreateSuccess(result, "Lấy danh sách brand của team thành công"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(GenericResponse<IEnumerable<BrandResponseDto>>.CreateError("Token không hợp lệ"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting brands for team {TeamId}", teamId);
                return StatusCode(500, GenericResponse<IEnumerable<BrandResponseDto>>.CreateError("Đã xảy ra lỗi khi lấy danh sách brand của team"));
            }
        }

        /// <summary>
        /// Tạo brand mới
        /// POST api/brands
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<GenericResponse<BrandResponseDto>>> Create([FromBody] CreateBrandRequest request)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);

                var created = await _brandService.CreateAsync(userId, request);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, GenericResponse<BrandResponseDto>.CreateSuccess(created, "Tạo brand thành công"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(GenericResponse<BrandResponseDto>.CreateError("Token không hợp lệ"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request for brand creation");
                return BadRequest(GenericResponse<BrandResponseDto>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating brand");
                return StatusCode(500, GenericResponse<BrandResponseDto>.CreateError("Đã xảy ra lỗi khi tạo brand"));
            }
        }

        /// <summary>
        /// Cập nhật brand theo Id
        /// PUT api/brands/{id}
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<BrandResponseDto>>> Update(Guid id, [FromBody] UpdateBrandRequest request)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);

                var updated = await _brandService.UpdateAsync(id, userId, request);
                if (updated == null)
                    return NotFound(GenericResponse<BrandResponseDto>.CreateError("Không tìm thấy brand hoặc không có quyền truy cập"));

                return Ok(GenericResponse<BrandResponseDto>.CreateSuccess(updated, "Cập nhật brand thành công"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(GenericResponse<BrandResponseDto>.CreateError("Token không hợp lệ"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request for brand update {BrandId}", id);
                return BadRequest(GenericResponse<BrandResponseDto>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating brand {BrandId}", id);
                return StatusCode(500, GenericResponse<BrandResponseDto>.CreateError("Đã xảy ra lỗi khi cập nhật brand"));
            }
        }

        /// <summary>
        /// Xóa mềm brand (soft delete)
        /// DELETE api/brands/{id}
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<object>>> SoftDelete(Guid id)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);

                var success = await _brandService.SoftDeleteAsync(id, userId);
                if (!success)
                    return NotFound(GenericResponse<object>.CreateError("Không tìm thấy brand hoặc không có quyền truy cập"));

                return Ok(GenericResponse<object>.CreateSuccess(null, "Xóa brand thành công"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(GenericResponse<object>.CreateError("Token không hợp lệ"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting brand {BrandId}", id);
                return StatusCode(500, GenericResponse<object>.CreateError("Đã xảy ra lỗi khi xóa brand"));
            }
        }

        /// <summary>
        /// Khôi phục brand đã xóa mềm
        /// POST api/brands/{id}/restore
        /// </summary>
        [HttpPost("{id}/restore")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<object>>> Restore(Guid id)
        {
            try
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(User);

                var ok = await _brandService.RestoreAsync(id, userId);
                if (!ok)
                    return NotFound(GenericResponse<object>.CreateError("Không tìm thấy brand hoặc không ở trạng thái đã xóa"));

                return Ok(GenericResponse<object>.CreateSuccess(null, "Khôi phục brand thành công"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(GenericResponse<object>.CreateError("Token không hợp lệ"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring brand {BrandId}", id);
                return StatusCode(500, GenericResponse<object>.CreateError("Đã xảy ra lỗi khi khôi phục brand"));
            }
        }
    }
}