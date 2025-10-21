using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Request;
using AISAM.Common.Dtos.Response;
using AISAM.Common.Models;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using AISAM.Services.Helper;
using System;

namespace AISAM.Services.Service
{
    public class BrandService : IBrandService
    {
        private readonly IBrandRepository _brandRepository;
        private readonly IProfileRepository _profileRepository;
        private readonly ITeamMemberRepository _teamMemberRepository;
        private readonly IProductRepository _productRepository;
        private readonly RolePermissionConfig _rolePermissionConfig;

        public BrandService(IBrandRepository brandRepository, IProfileRepository profileRepository, ITeamMemberRepository teamMemberRepository, IProductRepository productRepository, RolePermissionConfig rolePermissionConfig)
        {
            _brandRepository = brandRepository;
            _profileRepository = profileRepository;
            _teamMemberRepository = teamMemberRepository;
            _productRepository = productRepository;
            _rolePermissionConfig = rolePermissionConfig;
        }

        /// <summary>
        /// Lấy danh sách brand theo profileId với phân trang, hỗ trợ search và sắp xếp
        /// </summary>
        public async Task<PagedResult<BrandResponseDto>> GetPagedByProfileIdAsync(Guid profileId, PaginationRequest request)
        {
            var brands = await _brandRepository.GetPagedByProfileIdAsync(profileId, request);

            return new PagedResult<BrandResponseDto>
            {
                Data = brands.Data.Select(MapToResponse).ToList(),
                TotalCount = brands.TotalCount,
                Page = brands.Page,
                PageSize = brands.PageSize
            };
        }

        /// <summary>
        /// Lấy danh sách brand theo quyền truy cập qua team membership với phân trang, hỗ trợ search và sắp xếp
        /// </summary>
        public async Task<PagedResult<BrandResponseDto>> GetPagedBrandsByTeamMembershipAsync(Guid profileId, PaginationRequest request)
        {
            var brands = await _brandRepository.GetPagedBrandsByTeamMembershipAsync(profileId, request);

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
        public async Task<BrandResponseDto?> GetByIdAsync(Guid id, Guid profileId)
        {
            var brand = await _brandRepository.GetByIdAsync(id);
            if (brand == null || brand.IsDeleted) return null;

            // Check if profile has access to this brand (owner or team member)
            var hasAccess = await CanProfileAccessBrandAsync(profileId, id, "VIEW_TEAM_DETAILS");
            if (!hasAccess)
            {
                throw new UnauthorizedAccessException("You are not allowed to access this brand");
            }

            return MapToResponse(brand);
        }

        /// <summary>
        /// Tạo mới brand
        /// </summary>
        public async Task<BrandResponseDto> CreateAsync(Guid profileId, CreateBrandRequest dto)
        {
            // Kiểm tra Profile tồn tại
            if (!await _profileRepository.ExistsAsync(profileId))
                throw new ArgumentException("Profile not found.");

            var brand = new Brand
            {
                Id = Guid.NewGuid(),
                ProfileId = profileId,
                Name = dto.Name,
                Description = dto.Description,
                LogoUrl = dto.LogoUrl,
                Slogan = dto.Slogan,
                Usp = dto.Usp,
                TargetAudience = dto.TargetAudience,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _brandRepository.AddAsync(brand);
            return MapToResponse(created);
        }

        /// <summary>
        /// Cập nhật thông tin brand theo Id, kiểm tra quyền sở hữu
        /// </summary>
        public async Task<BrandResponseDto?> UpdateAsync(Guid id, Guid profileId, UpdateBrandRequest dto)
        {
            var brand = await _brandRepository.GetByIdAsync(id);
            if (brand == null || brand.IsDeleted) return null;

            // Check if profile has permission to update this brand (owner or team member with UPDATE_TEAM)
            var hasAccess = await CanProfileAccessBrandAsync(profileId, id, "UPDATE_TEAM");
            if (!hasAccess)
            {
                throw new UnauthorizedAccessException("You are not allowed to update this brand");
            }

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

            // ProfileId cannot be changed after creation

            brand.UpdatedAt = DateTime.UtcNow;

            await _brandRepository.UpdateAsync(brand);
            return MapToResponse(brand);
        }

        /// <summary>
        /// Soft delete brand (chỉ đánh dấu IsDeleted = true, không xóa thực sự), kiểm tra quyền sở hữu
        /// Cũng soft delete tất cả products thuộc brand này
        /// </summary>
        public async Task<bool> SoftDeleteAsync(Guid id, Guid profileId)
        {
            var brand = await _brandRepository.GetByIdAsync(id);
            if (brand == null || brand.IsDeleted) return false;

            // Check if profile has permission to delete this brand (owner or team member with DELETE_TEAM)
            var hasAccess = await CanProfileAccessBrandAsync(profileId, id, "DELETE_TEAM");
            if (!hasAccess)
            {
                throw new UnauthorizedAccessException("You are not allowed to delete this brand");
            }

            // Soft delete tất cả products thuộc brand này
            var products = await _productRepository.GetProductsByBrandIdAsync(id);
            foreach (var product in products)
            {
                product.IsDeleted = true;
                product.UpdatedAt = DateTime.UtcNow;
                await _productRepository.UpdateAsync(product);
            }

            brand.IsDeleted = true;
            brand.UpdatedAt = DateTime.UtcNow;
            await _brandRepository.UpdateAsync(brand);
            return true;
        }

        /// <summary>
        /// Khôi phục brand đã xóa mềm, kiểm tra quyền sở hữu
        /// Cũng khôi phục tất cả products đã xóa mềm cùng với brand này
        /// </summary>
        public async Task<bool> RestoreAsync(Guid id, Guid profileId)
        {
            var brand = await _brandRepository.GetByIdIncludingDeletedAsync(id);
            if (brand == null || !brand.IsDeleted) return false;

            // Check if profile has permission to restore this brand (owner or team member with DELETE_TEAM)
            var hasAccess = await CanProfileAccessBrandAsync(profileId, id, "DELETE_TEAM");
            if (!hasAccess)
            {
                throw new UnauthorizedAccessException("You are not allowed to restore this brand");
            }

            // Khôi phục tất cả products đã xóa mềm cùng với brand này
            var products = await _productRepository.GetProductsByBrandIdIncludingDeletedAsync(id);
            foreach (var product in products.Where(p => p.IsDeleted))
            {
                product.IsDeleted = false;
                product.UpdatedAt = DateTime.UtcNow;
                await _productRepository.UpdateAsync(product);
            }

            brand.IsDeleted = false;
            brand.UpdatedAt = DateTime.UtcNow;

            await _brandRepository.UpdateAsync(brand);
            return true;
        }

        /// <summary>
        /// Check if user can access brand with required permission
        /// </summary>
        private async Task<bool> CanProfileAccessBrandAsync(Guid profileId, Guid brandId, string requiredPermission)
        {
            var brand = await _brandRepository.GetByIdAsync(brandId);
            if (brand == null) return false;

            // Profile is brand owner
            if (brand.ProfileId == profileId)
            {
                return true;
            }

            // Check if profile is team member of brand owner with required permission
            var teamMember = await _teamMemberRepository.GetByUserIdAsync(profileId);
            if (teamMember == null) return false;

            // Check if team member belongs to the brand owner's profile
            if (teamMember.Team.ProfileId != brand.ProfileId) return false;

            // Check if team member has required permission
            return _rolePermissionConfig.HasCustomPermission(teamMember.Permissions, requiredPermission);
        }

        /// <summary>
        /// Chuyển Brand sang BrandResponseDto
        /// </summary>
        private static BrandResponseDto MapToResponse(Brand brand)
        {
            return new BrandResponseDto
            {
                Id = brand.Id,
                ProfileId = brand.ProfileId,
                Name = brand.Name,
                Description = brand.Description,
                LogoUrl = brand.LogoUrl,
                Slogan = brand.Slogan,
                Usp = brand.Usp,
                TargetAudience = brand.TargetAudience,
                CreatedAt = brand.CreatedAt,
                UpdatedAt = brand.UpdatedAt
            };
        }
    }
}