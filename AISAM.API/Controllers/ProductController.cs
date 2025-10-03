using Microsoft.AspNetCore.Mvc;
using AISAM.Common;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using AISAM.Common.Models;

namespace AISAM.API.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _service;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IProductService service, ILogger<ProductController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<ProductResponseDto>>> GetById(Guid id)
        {
            try
            {
                var product = await _service.GetByIdAsync(id);
                if (product == null)
                {
                    return NotFound(GenericResponse<ProductResponseDto>.CreateError("Không tìm thấy product"));
                }

                return Ok(GenericResponse<ProductResponseDto>.CreateSuccess(product, "Lấy product thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product {ProductId}", id);
                return StatusCode(500, GenericResponse<ProductResponseDto>.CreateError(
                    "Đã xảy ra lỗi khi lấy product"
                ));
            }
        }

        /// <summary>
        /// Get paginated list of products
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<GenericResponse<PagedResult<ProductResponseDto>>>> GetProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDescending = true)
        {
            try
            {
                var paginationRequest = new PaginationRequest
                {
                    Page = page,
                    PageSize = pageSize,
                    SearchTerm = searchTerm,
                    SortBy = sortBy,
                    SortDescending = sortDescending
                };

                var result = await _service.GetPagedAsync(paginationRequest);

                return Ok(GenericResponse<PagedResult<ProductResponseDto>>.CreateSuccess(
                    result,
                    "Lấy danh sách product thành công"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated products");
                return StatusCode(500, GenericResponse<PagedResult<ProductResponseDto>>.CreateError(
                    "Đã xảy ra lỗi khi lấy danh sách product"
                ));
            }
        }

        [HttpPost]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<GenericResponse<ProductResponseDto>>> Create([FromForm] ProductCreateRequest request)
        {
            try
            {
                var created = await _service.CreateAsync(request, request.ImageFiles);
                return CreatedAtAction(nameof(GetById), new { id = created.Id },
                    GenericResponse<ProductResponseDto>.CreateSuccess(created, "Tạo product thành công"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request for product creation");
                return BadRequest(GenericResponse<ProductResponseDto>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(500, GenericResponse<ProductResponseDto>.CreateError("Đã xảy ra lỗi khi tạo product"));
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<GenericResponse<ProductResponseDto>>> Update(Guid id, [FromForm] ProductUpdateRequestDto request)
        {
            try
            {
                var updated = await _service.UpdateAsync(id, request);
                if (updated == null)
                {
                    return NotFound(GenericResponse<ProductResponseDto>.CreateError(
                        "Không tìm thấy product"));
                }

                return Ok(GenericResponse<ProductResponseDto>.CreateSuccess(updated, "Cập nhật product thành công"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request for product update {ProductId}", id);
                return BadRequest(GenericResponse<ProductResponseDto>.CreateError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", id);
                return StatusCode(500, GenericResponse<ProductResponseDto>.CreateError("Đã xảy ra lỗi khi cập nhật product"));
            }
        }


        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<object>>> SoftDelete(Guid id)
        {
            try
            {
                var success = await _service.SoftDeleteAsync(id);
                if (!success)
                {
                    return NotFound(GenericResponse<object>.CreateError(
                        "Không tìm thấy product hoặc đã bị xóa"));
                }

                return Ok(GenericResponse<object>.CreateSuccess(null, "Xóa product thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                return StatusCode(500, GenericResponse<object>.CreateError("Đã xảy ra lỗi khi xóa product"));
            }
        }

        [HttpPost("{id}/restore")]
        [Authorize]
        public async Task<ActionResult<GenericResponse<object>>> Restore(Guid id)
        {
            try
            {
                var ok = await _service.RestoreAsync(id);
                if (!ok)
                {
                    return NotFound(GenericResponse<object>.CreateError("Không tìm thấy product hoặc không ở trạng thái đã xóa"));
                }
                return Ok(GenericResponse<object>.CreateSuccess(null, "Khôi phục product thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring product {ProductId}", id);
                return StatusCode(500, GenericResponse<object>.CreateError("Đã xảy ra lỗi khi khôi phục product"));
            }
        }
    }
}
