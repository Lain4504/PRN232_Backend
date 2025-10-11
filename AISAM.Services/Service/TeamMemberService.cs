using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Response;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.Helper;
using AISAM.Services.IServices;
using Microsoft.Extensions.Logging;

namespace AISAM.Services.Service
{
    public class TeamMemberService : ITeamMemberService
    {
        private readonly ITeamMemberRepository _teamMemberRepository;
        private readonly RolePermissionConfig _rolePermissionConfig;
        private readonly ILogger<TeamMemberService> _logger;

        public TeamMemberService(
            ITeamMemberRepository teamMemberRepository,
            RolePermissionConfig rolePermissionConfig,
            ILogger<TeamMemberService> logger)
        {
            _teamMemberRepository = teamMemberRepository;
            _rolePermissionConfig = rolePermissionConfig;
            _logger = logger;
        }

        public async Task<PagedResult<TeamMemberResponseDto>> GetPagedAsync(PaginationRequest request)
        {
            var result = await _teamMemberRepository.GetPagedAsync(request);
            return new PagedResult<TeamMemberResponseDto>
            {
                Data = result.Data.Select(MapToResponse).ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<TeamMemberResponseDto?> GetByIdAsync(Guid id, Guid userId)
        {
            var currentUserMember = await _teamMemberRepository.GetByUserIdAsync(userId);

            if (currentUserMember == null)
            {
                throw new ArgumentException("Không tìm thấy thành viên hiện tại.");
            }

            if (!_rolePermissionConfig.HasCustomPermission(currentUserMember.Permissions, "VIEW_TEAM_MEMBER_DETAILS"))
                throw new UnauthorizedAccessException("Bạn không có quyền xem chi tiết thành viên.");

            var entity = await _teamMemberRepository.GetByIdAsync(id);
            return entity == null ? null : MapToResponse(entity);
        }

        public async Task<TeamMemberResponseDto> CreateAsync(TeamMemberCreateRequest request, Guid userId)
        {
            var currentUserMember = await _teamMemberRepository.GetByUserIdAsync(userId);

            if (currentUserMember == null)
            {
                throw new ArgumentException("Không tìm thấy thành viên hiện tại.");
            }

            if (!_rolePermissionConfig.HasCustomPermission(currentUserMember.Permissions, "ADD_MEMBER"))
                throw new InvalidOperationException("Bạn không có quyền thêm thành viên.");

            if (!await _teamMemberRepository.TeamExistsAsync(request.TeamId))
                throw new ArgumentException("Team không tồn tại.");

            if (!await _teamMemberRepository.UserExistsAsync(request.UserId))
                throw new ArgumentException("User không tồn tại.");

            // Validate that all permissions belong to the assigned role
            if (request.Permissions != null && request.Permissions.Any())
            {
                ValidatePermissionsForRole(request.Role, request.Permissions);
            }

            var entity = new TeamMember
            {
                Id = Guid.NewGuid(),
                TeamId = request.TeamId,
                UserId = request.UserId,
                Role = request.Role, // đã đổi sang string
                Permissions = request.Permissions ?? new List<string>(),
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };

            var created = await _teamMemberRepository.AddAsync(entity);
            _logger.LogInformation("Tạo team member thành công: {Id}", created.Id);
            return MapToResponse(created);
        }

        public async Task<TeamMemberResponseDto?> UpdateAsync(Guid id, TeamMemberUpdateRequest request, Guid userId)
        {
            var currentUserMember = await _teamMemberRepository.GetByUserIdAsync(userId);
            
            if (currentUserMember == null)
            {
                throw new ArgumentException("Không tìm thấy thành viên hiện tại.");
            }

            if (!_rolePermissionConfig.HasCustomPermission(currentUserMember.Permissions, "UPDATE_MEMBER_ROLE"))
                throw new UnauthorizedAccessException("Bạn không có quyền cập nhật thành viên.");

            var entity = await _teamMemberRepository.GetByIdAsync(id);
            if (entity == null)
                throw new ArgumentException("Không tìm thấy thành viên.");

            if (!string.IsNullOrEmpty(request.TeamId))
            {
                if (!await _teamMemberRepository.TeamExistsAsync(Guid.Parse(request.TeamId)))
                    throw new ArgumentException("Team không tồn tại.");
                entity.TeamId = Guid.Parse(request.TeamId);
            }

            if (!string.IsNullOrEmpty(request.Role))
                entity.Role = request.Role;

            if (request.Permissions != null && request.Permissions.Any())
            {
                // Validate that all permissions belong to the assigned role
                var roleToValidate = !string.IsNullOrEmpty(request.Role) ? request.Role : entity.Role;
                ValidatePermissionsForRole(roleToValidate, request.Permissions);
                entity.Permissions = request.Permissions;
            }

            if (request.IsActive.HasValue)
                entity.IsActive = request.IsActive.Value;

            await _teamMemberRepository.UpdateAsync(entity);
            _logger.LogInformation("Cập nhật team member thành công: {Id}", id);
            return MapToResponse(entity);
        }

        public async Task<bool> DeleteAsync(Guid id, Guid userId)
        {
            var currentUserMember = await _teamMemberRepository.GetByUserIdAsync(userId);
            
            if (currentUserMember == null)
            {
                throw new ArgumentException("Không tìm thấy thành viên hiện tại.");
            }

            if (!_rolePermissionConfig.HasCustomPermission(currentUserMember.Permissions, "REMOVE_MEMBER"))
                throw new UnauthorizedAccessException("Bạn không có quyền xóa thành viên.");

            var result = await _teamMemberRepository.DeleteAsync(id);
            if (result)
                _logger.LogInformation("Đã xóa team member {Id}", id);
            return result;
        }

        private static TeamMemberResponseDto MapToResponse(TeamMember entity)
        {
            return new TeamMemberResponseDto
            {
                Id = entity.Id,
                TeamId = entity.TeamId,
                UserId = entity.UserId,
                Role = entity.Role,
                Permissions = entity.Permissions,
                JoinedAt = entity.JoinedAt,
                IsActive = entity.IsActive
            };
        }

        /// <summary>
        /// Validate that all permissions belong to the specified role
        /// </summary>
        private void ValidatePermissionsForRole(string role, List<string> permissions)
        {
            if (string.IsNullOrEmpty(role))
                throw new ArgumentException("Role không được để trống.");

            // Get all permissions that belong to this role
            var rolePermissions = _rolePermissionConfig.GetPermissionsByRole(role);

            // Check if all provided permissions belong to the role
            var invalidPermissions = permissions.Where(p => 
                !rolePermissions.Any(rp => string.Equals(rp, p, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            if (invalidPermissions.Any())
            {
                var invalidPermissionsList = string.Join(", ", invalidPermissions);
                throw new ArgumentException(
                    $"Các quyền sau không thuộc về role '{role}': {invalidPermissionsList}. " +
                    $"Các quyền hợp lệ cho role '{role}' là: {string.Join(", ", rolePermissions)}"
                );
            }
        }
    }
}
