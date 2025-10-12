using AISAM.Common.DTOs.Request;
using AISAM.Common.DTOs.Response;
using AISAM.Common;
using AISAM.Data.Model;
using AISAM.Data.Enumeration;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using AISAM.Services.Helper;
using AISAM.Common.Dtos;
using AISAM.Common.Dtos.Response;
namespace AISAM.Services.Service
{
    public class TeamService : ITeamService
    {
        private readonly ITeamRepository _teamRepository;
        private readonly ITeamMemberRepository _teamMemberRepository;
        private readonly IUserRepository _userRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly ITeamBrandRepository _teamBrandRepository;
        private readonly RolePermissionConfig _rolePermissionConfig;

        public TeamService(ITeamMemberRepository teamMemberRepository, ITeamRepository teamRepository, IUserRepository userRepository, IBrandRepository brandRepository, ITeamBrandRepository teamBrandRepository, RolePermissionConfig rolePermissionConfig)
        {
            _teamMemberRepository = teamMemberRepository;
            _teamRepository = teamRepository;
            _userRepository = userRepository;
            _brandRepository = brandRepository;
            _teamBrandRepository = teamBrandRepository;
            _rolePermissionConfig = rolePermissionConfig;
        }

        public async Task<GenericResponse<TeamResponse>> CreateTeamAsync(CreateTeamRequest request, Guid userId)
        {
            try
            {
                // Kiểm tra quyền tạo team: user phải có role Vendor 
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null || user.Role != UserRoleEnum.Vendor)
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền tạo team.");
                }

                // Kiểm tra tên team đã tồn tại chưa
                var existingTeam = await _teamRepository.ExistsByNameAndVendorAsync(request.Name, userId);
                if (existingTeam)
                {
                    return GenericResponse<TeamResponse>.CreateError("Tên team đã tồn tại");
                }

                // Tạo team mới
                var team = new Team
                {
                    Id = Guid.NewGuid(),
                    VendorId = userId,
                    Name = request.Name.Trim(),
                    Description = request.Description?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    Status = TeamStatusEnum.Active
                };

                var createdTeam = await _teamRepository.CreateAsync(team);
                if (request.BrandIds?.Any() == true)
                {
                    await _teamBrandRepository.CreateTeamBrandAssociationsAsync(createdTeam.Id, request.BrandIds, userId);
                }

                try
                {
                    // Tạo TeamMember cho người tạo team với role Vendor
                    var teamMember = new TeamMember
                    {
                        Id = Guid.NewGuid(),
                        TeamId = createdTeam.Id,
                        UserId = userId,
                        Role = "Vendor",
                        Permissions = _rolePermissionConfig.GetPermissionsByRole("Vendor"),
                        JoinedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    await _teamMemberRepository.AddAsync(teamMember);
                }
                catch (Exception ex)
                {
                    // Log lỗi tạo TeamMember nhưng không làm fail việc tạo team
                    Console.WriteLine($"Warning: Failed to create team member for user {userId} in team {createdTeam.Id}: {ex.Message}");
                }

                var response = new TeamResponse
                {
                    Id = createdTeam.Id,
                    VendorId = createdTeam.VendorId,
                    Name = createdTeam.Name,
                    Description = createdTeam.Description,
                    CreatedAt = createdTeam.CreatedAt,
                    VendorEmail = user?.Email ?? "",
                };

                return GenericResponse<TeamResponse>.CreateSuccess(response, "Tạo team thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<TeamResponse>.CreateError($"Lỗi khi lấy thông tin team: {ex.Message}");
            }
        }

        public async Task<GenericResponse<TeamResponse>> GetTeamByIdAsync(Guid id, Guid userId)
        {
            try
            {
                var currentUserPermissions = await GetPermissionsByUserId(userId);
                if (!_rolePermissionConfig.HasCustomPermission(currentUserPermissions, "VIEW_TEAM_MEMBERS"))
                    throw new UnauthorizedAccessException("Bạn không có quyền xem thông tin team.");

                // Lấy thông tin team theo ID
                var team = await _teamRepository.GetByIdAsync(id);
                if (team == null)
                {
                    return GenericResponse<TeamResponse>.CreateError("Không tìm thấy team với ID được cung cấp.");
                }

                // Lấy thông tin vendor để có email
                var vendor = await _userRepository.GetByIdAsync(team.VendorId);
                var response = new TeamResponse
                {
                    Id = team.Id,
                    VendorId = team.VendorId,
                    Name = team.Name,
                    Description = team.Description,
                    CreatedAt = team.CreatedAt,
                    VendorEmail = vendor?.Email ?? "",
                };

                return GenericResponse<TeamResponse>.CreateSuccess(response, "Lấy thông tin team thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<TeamResponse>.CreateError($"Lỗi khi lấy thông tin team: {ex.Message}");
            }
        }

        public async Task<GenericResponse<IEnumerable<TeamResponse>>> GetTeamsByVendorAsync(Guid vendorId, Guid userId)
        {
            try
            {
                var currentUserPermissions = await GetPermissionsByUserId(userId);
                if (!_rolePermissionConfig.HasCustomPermission(currentUserPermissions, "VIEW_TEAM_MEMBERS"))
                    throw new UnauthorizedAccessException("Bạn không có quyền xem danh sách team.");

                // Lấy danh sách teams theo vendor
                var teams = await _teamRepository.GetByVendorIdAsync(vendorId, userId);

                // Lấy thông tin vendor để có email
                var vendor = await _userRepository.GetByIdAsync(vendorId);

                // Chuyển đổi sang TeamResponse với thông tin đầy đủ
                var teamResponses = new List<TeamResponse>();
                foreach (var team in teams)
                {
                    teamResponses.Add(new TeamResponse
                    {
                        Id = team.Id,
                        VendorId = team.VendorId,
                        Name = team.Name,
                        Description = team.Description,
                        CreatedAt = team.CreatedAt,
                        VendorEmail = vendor?.Email ?? "",
                    });
                }

                return GenericResponse<IEnumerable<TeamResponse>>.CreateSuccess(teamResponses, "Lấy danh sách team thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<IEnumerable<TeamResponse>>.CreateError($"Lỗi khi lấy danh sách team: {ex.Message}");
            }
        }

        public async Task<GenericResponse<TeamResponse>> UpdateTeamAsync(Guid id, CreateTeamRequest request, Guid userId)
        {
            try
            {
                var currentUserPermissions = await GetPermissionsByUserId(userId);
                if (!_rolePermissionConfig.HasCustomPermission(currentUserPermissions, "UPDATE_TEAM"))
                    throw new UnauthorizedAccessException("Bạn không có quyền cập nhật team.");

                // Lấy thông tin team hiện tại
                var existingTeam = await _teamRepository.GetByIdAsync(id);
                if (existingTeam == null)
                {
                    return GenericResponse<TeamResponse>.CreateError("Không tìm thấy team để cập nhật.");
                }

                // Kiểm tra tên team mới có bị trùng không (chỉ khi tên thay đổi)
                if (existingTeam.Name.Trim() != request.Name.Trim())
                {
                    var nameExists = await _teamRepository.ExistsByNameAndVendorAsync(request.Name, existingTeam.VendorId);
                    if (nameExists)
                    {
                        return GenericResponse<TeamResponse>.CreateError("Tên team đã tồn tại");
                    }
                }

                // Cập nhật thông tin team
                existingTeam.Name = request.Name.Trim();
                existingTeam.Description = request.Description?.Trim();

                var updatedTeam = await _teamRepository.UpdateAsync(existingTeam);

                // Lấy thông tin vendor để có email
                var vendor = await _userRepository.GetByIdAsync(updatedTeam.VendorId);

                var response = new TeamResponse
                {
                    Id = updatedTeam.Id,
                    VendorId = updatedTeam.VendorId,
                    Name = updatedTeam.Name,
                    Description = updatedTeam.Description,
                    CreatedAt = updatedTeam.CreatedAt,
                    VendorEmail = vendor?.Email ?? "",
                };

                return GenericResponse<TeamResponse>.CreateSuccess(response, "Cập nhật team thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<TeamResponse>.CreateError($"Lỗi khi cập nhật team: {ex.Message}");
            }
        }

        public async Task<GenericResponse<bool>> DeleteTeamAsync(Guid id, Guid userId)
        {
            try
            {
                var currentUserPermissions = await GetPermissionsByUserId(userId);
                if (!_rolePermissionConfig.HasCustomPermission(currentUserPermissions, "DELETE_TEAM"))
                    throw new UnauthorizedAccessException("Bạn không có quyền xóa team.");

                // Lấy thông tin team hiện tại để kiểm tra tồn tại
                var existingTeam = await _teamRepository.GetByIdAsync(id);
                if (existingTeam == null)
                {
                    return GenericResponse<bool>.CreateError("Không tìm thấy team để xóa.");
                }

                // Kiểm tra team đã bị xóa mềm chưa
                if (existingTeam.IsDeleted)
                {
                    return GenericResponse<bool>.CreateError("Team đã được xóa trước đó.");
                }

                // Soft delete tất cả team members trước
                var deletedMembersCount = await _teamMemberRepository.SoftDeleteByTeamIdAsync(id);
                if (deletedMembersCount > 0)
                {
                    Console.WriteLine($"Soft deleted {deletedMembersCount} team members for team {id}");
                }

                // Soft delete tất cả team brand associations trước
                var deletedBrandsCount = await _teamBrandRepository.SoftDeleteByTeamIdAsync(id);
                if (deletedBrandsCount > 0)
                {
                    Console.WriteLine($"Soft deleted {deletedBrandsCount} team brand associations for team {id}");
                }

                // Soft delete team từ repository
                existingTeam.IsDeleted = true;
                await _teamRepository.UpdateAsync(existingTeam);

                return GenericResponse<bool>.CreateSuccess(true, "Xóa team thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<bool>.CreateError($"Lỗi khi xóa team: {ex.Message}");
            }
        }

        public async Task<GenericResponse<IEnumerable<TeamMemberResponseDto>>> GetTeamMembersAsync(Guid teamId, Guid userId)
        {
            try
            {
                var currentUserPermissions = await GetPermissionsByUserId(userId);
                if (!_rolePermissionConfig.HasCustomPermission(currentUserPermissions, "VIEW_TEAM_MEMBERS"))
                    throw new UnauthorizedAccessException("Bạn không có quyền xem danh sách thành viên trong team.");

                // Lấy thông tin team để kiểm tra tồn tại
                var team = await _teamRepository.GetByIdAsync(teamId);
                if (team == null)
                {
                    return GenericResponse<IEnumerable<TeamMemberResponseDto>>.CreateError("Không tìm thấy team với ID được cung cấp.");
                }

                // Lấy danh sách thành viên trong team
                var teamMembers = await _teamMemberRepository.GetByTeamIdAsync(teamId);

                // Lấy thông tin user cho mỗi thành viên để có email
                var memberResponses = new List<TeamMemberResponseDto>();
                foreach (var member in teamMembers)
                {
                    var user = await _userRepository.GetByIdAsync(member.UserId);
                    memberResponses.Add(new TeamMemberResponseDto
                    {
                        Id = member.Id,
                        TeamId = member.TeamId,
                        UserId = member.UserId,
                        Role = member.Role,
                        Permissions = member.Permissions ?? new List<string>(),
                        JoinedAt = member.JoinedAt,
                        IsActive = member.IsActive,
                        UserEmail = user?.Email ?? ""
                    });
                }

                return GenericResponse<IEnumerable<TeamMemberResponseDto>>.CreateSuccess(memberResponses, "Lấy danh sách thành viên thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<IEnumerable<TeamMemberResponseDto>>.CreateError($"Lỗi khi lấy danh sách thành viên: {ex.Message}");
            }
        }

        private async Task<List<string>> GetPermissionsByUserId(Guid userId)
        {
            var member = await _teamMemberRepository.GetByUserIdAsync(userId);
            if (member == null || member.Permissions == null || member.Permissions.Count == 0)
                return new List<string> {}; // default permissions
            return member.Permissions;
        }
    }
}