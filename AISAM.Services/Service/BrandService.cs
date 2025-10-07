using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Common.Models;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;

namespace AISAM.Services.Service
{
    public class BrandService : IBrandService
    {
        private readonly IBrandRepository _brandRepository;

        public BrandService(IBrandRepository brandRepository)
        {
            _brandRepository = brandRepository;
        }

        /// <summary>
        /// Lấy danh sách brand theo userId với phân trang, hỗ trợ search và sắp xếp
        /// </summary>
        public async Task<PagedResult<BrandResponseDto>> GetPagedByUserIdAsync(Guid userId, PaginationRequest request)
        {
            var brands = await _brandRepository.GetPagedByUserIdAsync(userId, request);

            return new PagedResult<BrandResponseDto>
            {
                Data = brands.Data.Select(MapToResponse).ToList(),
                TotalCount = brands.TotalCount,
                Page = brands.Page,
                PageSize = brands.PageSize
            };
        }

        /// <summary>
        /// Lấy thông tin chi tiết brand theo Id
        /// </summary>
        public async Task<BrandResponseDto?> GetByIdAsync(Guid id)
        {
            var brand = await _brandRepository.GetByIdAsync(id);
            if (brand == null || brand.IsDeleted) return null;

            return MapToResponse(brand);
        }

        /// <summary>
        /// Tạo mới brand
        /// </summary>
        public async Task<BrandResponseDto> CreateAsync(Guid userId, CreateBrandRequest dto)
        {
            // Kiểm tra User tồn tại
            if (!await _brandRepository.UserExistsAsync(userId))
                throw new ArgumentException("User not found.");

            var brand = new Brand
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = dto.Name,
                Description = dto.Description,
                LogoUrl = dto.LogoUrl,
                Slogan = dto.Slogan,
                Usp = dto.Usp,
                TargetAudience = dto.TargetAudience,
                ProfileId = dto.ProfileId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _brandRepository.AddAsync(brand);
            return MapToResponse(created);
        }

        /// <summary>
        /// Cập nhật thông tin brand theo Id, kiểm tra quyền sở hữu
        /// </summary>
        public async Task<BrandResponseDto?> UpdateAsync(Guid id, Guid userId, UpdateBrandRequest dto)
        {
            var brand = await _brandRepository.GetByIdAsync(id);
            if (brand == null || brand.IsDeleted || brand.UserId != userId) return null;

            if (!string.IsNullOrWhiteSpace(dto.Name))
                brand.Name = dto.Name;

            if (!string.IsNullOrWhiteSpace(dto.Description))
                brand.Description = dto.Description;

            if (!string.IsNullOrWhiteSpace(dto.LogoUrl))
                brand.LogoUrl = dto.LogoUrl;

            if (!string.IsNullOrWhiteSpace(dto.Slogan))
                brand.Slogan = dto.Slogan;

            if (!string.IsNullOrWhiteSpace(dto.Usp))
                brand.Usp = dto.Usp;

            if (!string.IsNullOrWhiteSpace(dto.TargetAudience))
                brand.TargetAudience = dto.TargetAudience;

            if (dto.ProfileId.HasValue)
                brand.ProfileId = dto.ProfileId;

            brand.UpdatedAt = DateTime.UtcNow;

            await _brandRepository.UpdateAsync(brand);
            return MapToResponse(brand);
        }

        /// <summary>
        /// Soft delete brand (chỉ đánh dấu IsDeleted = true, không xóa thực sự), kiểm tra quyền sở hữu
        /// </summary>
        public async Task<bool> SoftDeleteAsync(Guid id, Guid userId)
        {
            var brand = await _brandRepository.GetByIdAsync(id);
            if (brand == null || brand.IsDeleted || brand.UserId != userId) return false;

            brand.IsDeleted = true;
            brand.UpdatedAt = DateTime.UtcNow;
            await _brandRepository.UpdateAsync(brand);
            return true;
        }

        /// <summary>
        /// Khôi phục brand đã xóa mềm, kiểm tra quyền sở hữu
        /// </summary>
        public async Task<bool> RestoreAsync(Guid id, Guid userId)
        {
            var brand = await _brandRepository.GetByIdIncludingDeletedAsync(id);
            if (brand == null || !brand.IsDeleted || brand.UserId != userId) return false;

            brand.IsDeleted = false;
            brand.UpdatedAt = DateTime.UtcNow;

            await _brandRepository.UpdateAsync(brand);
            return true;
        }

        /// <summary>
        /// Chuyển Brand sang BrandResponseDto
        /// </summary>
        private static BrandResponseDto MapToResponse(Brand brand)
        {
            return new BrandResponseDto
            {
                Id = brand.Id,
                UserId = brand.UserId,
                Name = brand.Name,
                Description = brand.Description,
                LogoUrl = brand.LogoUrl,
                Slogan = brand.Slogan,
                Usp = brand.Usp,
                TargetAudience = brand.TargetAudience,
                ProfileId = brand.ProfileId,
                CreatedAt = brand.CreatedAt,
                UpdatedAt = brand.UpdatedAt
            };
        }
    }
}