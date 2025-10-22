using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Response;
using AISAM.Common;
using AISAM.Data.Model;
using AISAM.Data.Enumeration;
using AISAM.Repositories.IRepositories;
using AISAM.Services.Helper;
using AISAM.Services.IServices;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace AISAM.Services.Service
{
    public class TeamMemberService : ITeamMemberService
    {
        private readonly ITeamMemberRepository _teamMemberRepository;
        private readonly ITeamRepository _teamRepository;
        private readonly IProfileRepository _profileRepository;
        private readonly RolePermissionConfig _rolePermissionConfig;
        private readonly ILogger<TeamMemberService> _logger;

        public TeamMemberService(
            ITeamMemberRepository teamMemberRepository,
            ITeamRepository teamRepository,
            IProfileRepository profileRepository,
            RolePermissionConfig rolePermissionConfig,
            ILogger<TeamMemberService> logger)
        {
            _teamMemberRepository = teamMemberRepository;
            _teamRepository = teamRepository;
            _profileRepository = profileRepository;
            _rolePermissionConfig = rolePermissionConfig;
            _logger = logger;
        }

        public async Task<TeamMemberResponseDto?> GetByIdAsync(Guid id, Guid userId)
        {
            try
            {
                // Lấy thông tin team member cần xem
                var teamMember = await _teamMemberRepository.GetByIdAsync(id);
                if (teamMember == null)
                {
                    throw new ArgumentException("Không tìm thấy thành viên.");
                }

                // Validate quyền của user với team
                var permissionCheck = await ValidateUserTeamPermission(userId, teamMember.TeamId, "VIEW_TEAM_MEMBERS");
                if (!permissionCheck.Success)
                {
                    throw new UnauthorizedAccessException(permissionCheck.Message);
                }

                return MapToResponse(teamMember);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team member {MemberId} by user {UserId}", id, userId);
                throw;
            }
        }

        public async Task<TeamMemberResponseDto> CreateAsync(TeamMemberCreateRequest request, Guid userId)
        {
            try
            {
                // Validate quyền của user với team
                var permissionCheck = await ValidateUserTeamPermission(userId, request.TeamId, "ADD_MEMBER");
                if (!permissionCheck.Success)
                {
                    throw new UnauthorizedAccessException(permissionCheck.Message);
                }

                // Kiểm tra team tồn tại
                var team = await _teamRepository.GetByIdAsync(request.TeamId);
                if (team == null || team.IsDeleted)
                {
                    throw new ArgumentException("Team không tồn tại hoặc đã bị xóa.");
                }

                if (!await _teamMemberRepository.UserExistsAsync(request.UserId))
                    throw new ArgumentException("User không tồn tại.");

                // Kiểm tra user đã là member của team chưa
                var existingMember = await _teamMemberRepository.GetByTeamAndUserAsync(request.TeamId, request.UserId);
                if (existingMember != null && existingMember.IsActive)
                {
                    throw new ArgumentException("User đã là thành viên của team này.");
                }

                // Validate permissions nếu được cung cấp, nếu không thì dùng permissions mặc định của role
                List<string> finalPermissions;
                if (request.Permissions != null)
                {
                    // Nếu permissions được cung cấp, validate chúng phải thuộc về role
                    ValidatePermissionsForRole(request.Role, request.Permissions);
                    finalPermissions = request.Permissions;
                }
                else
                {
                    // Nếu không cung cấp permissions, dùng permissions mặc định của role
                    finalPermissions = _rolePermissionConfig.GetPermissionsByRole(request.Role);
                }

                var entity = new TeamMember
                {
                    Id = Guid.NewGuid(),
                    TeamId = request.TeamId,
                    UserId = request.UserId,
                    Role = request.Role,
                    Permissions = finalPermissions,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                };

                var created = await _teamMemberRepository.AddAsync(entity);
                _logger.LogInformation("Tạo team member thành công: {Id}", created.Id);
                return MapToResponse(created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating team member for user {UserId} in team {TeamId}", request.UserId, request.TeamId);
                throw;
            }
        }

        public async Task<TeamMemberResponseDto?> UpdateAsync(Guid id, TeamMemberUpdateRequest request, Guid userId)
        {
            try
            {
                var entity = await _teamMemberRepository.GetByIdAsync(id);
                if (entity == null)
                    throw new ArgumentException("Không tìm thấy thành viên.");

                // Validate quyền của user với team
                var permissionCheck = await ValidateUserTeamPermission(userId, entity.TeamId, "UPDATE_MEMBER_ROLE");
                if (!permissionCheck.Success)
                {
                    throw new UnauthorizedAccessException(permissionCheck.Message);
                }

                if (!string.IsNullOrEmpty(request.TeamId))
                {
                    var newTeamId = Guid.Parse(request.TeamId);
                    
                    // Validate quyền với team mới nếu team thay đổi
                    if (newTeamId != entity.TeamId)
                    {
                        var newTeamPermissionCheck = await ValidateUserTeamPermission(userId, newTeamId, "UPDATE_MEMBER_ROLE");
                        if (!newTeamPermissionCheck.Success)
                        {
                            throw new UnauthorizedAccessException(newTeamPermissionCheck.Message);
                        }
                    }

                    var team = await _teamRepository.GetByIdAsync(newTeamId);
                    if (team == null || team.IsDeleted)
                        throw new ArgumentException("Team không tồn tại hoặc đã bị xóa.");
                        
                    entity.TeamId = newTeamId;
                }

                if (!string.IsNullOrEmpty(request.Role))
                    entity.Role = request.Role;

                if (request.Permissions != null)
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating team member {MemberId} by user {UserId}", id, userId);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id, Guid userId)
        {
            try
            {
                var entity = await _teamMemberRepository.GetByIdAsync(id);
                if (entity == null)
                    throw new ArgumentException("Không tìm thấy thành viên để xóa.");

                // Validate quyền của user với team
                var permissionCheck = await ValidateUserTeamPermission(userId, entity.TeamId, "REMOVE_MEMBER");
                if (!permissionCheck.Success)
                {
                    throw new UnauthorizedAccessException(permissionCheck.Message);
                }

                var result = await _teamMemberRepository.DeleteAsync(id);
                if (result)
                    _logger.LogInformation("Đã xóa team member {Id} by user {UserId}", id, userId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting team member {MemberId} by user {UserId}", id, userId);
                throw;
            }
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
        /// Validate quyền của user với team - kiểm tra có phải người tạo team hoặc member có permission
        /// </summary>
        private async Task<GenericResponse<bool>> ValidateUserTeamPermission(Guid userId, Guid teamId, string requiredPermission)
        {
            try
            {
                // Lấy thông tin team hiện tại
                var existingTeam = await _teamRepository.GetByIdAsync(teamId);
                if (existingTeam == null)
                {
                    return GenericResponse<bool>.CreateError("Không tìm thấy team.");
                }

                // Kiểm tra quyền: user có phải là người tạo team hay không
                var userProfiles = await _profileRepository.GetByUserIdAsync(userId);
                var proProfiles = userProfiles?.Where(p => p.ProfileType == ProfileTypeEnum.Pro && !p.IsDeleted) ?? new List<Profile>();
                
                // Kiểm tra user có phải là người tạo team (có profile Pro nào thuộc về team này không)
                bool isTeamCreator = proProfiles.Any() && proProfiles.Any(p => p.Id == existingTeam.ProfileId);
                
                if (isTeamCreator)
                {
                    // Người tạo team có full quyền
                    return GenericResponse<bool>.CreateSuccess(true, "User có quyền thực hiện thao tác.");
                }
                else
                {
                    // Nếu không phải người tạo team, kiểm tra có phải member không và có permission không
                    var teamMember = await _teamMemberRepository.GetByTeamAndUserAsync(teamId, userId);
                    if (teamMember == null || !teamMember.IsActive)
                    {
                        return GenericResponse<bool>.CreateError("Bạn không có quyền thực hiện thao tác này.");
                    }

                    var currentUserPermissions = teamMember.Permissions ?? new List<string>();
                    if (!_rolePermissionConfig.HasCustomPermission(currentUserPermissions, requiredPermission))
                    {
                        return GenericResponse<bool>.CreateError("Bạn không có quyền thực hiện thao tác này.");
                    }

                    return GenericResponse<bool>.CreateSuccess(true, "User có quyền thực hiện thao tác.");
                }
            }
            catch (Exception ex)
            {
                return GenericResponse<bool>.CreateError($"Lỗi khi validate quyền: {ex.Message}");
            }
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
