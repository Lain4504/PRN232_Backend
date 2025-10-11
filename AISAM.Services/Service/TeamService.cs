using AISAM.Common.DTOs.Request;
using AISAM.Common.DTOs.Response;
using AISAM.Common;
using AISAM.Data.Model;
using AISAM.Repositories.IRepositories;
using AISAM.Services.IServices;
using AISAM.Services.Config;

namespace AISAM.Services.Service
{
    public class TeamService : ITeamService
    {
        private readonly ITeamRepository _teamRepository;
        private readonly ITeamMemberRepository _teamMemberRepository;
        private readonly IUserRepository _userRepository;
        private readonly RolePermissionConfig _rolePermissionConfig;

        public TeamService(ITeamMemberRepository teamMemberRepository, ITeamRepository teamRepository, IUserRepository userRepository, RolePermissionConfig rolePermissionConfig)
        {
            _teamMemberRepository = teamMemberRepository;
            _teamRepository = teamRepository;
            _userRepository = userRepository;
            _rolePermissionConfig = rolePermissionConfig;
        }

        public async Task<GenericResponse<TeamResponse>> CreateTeamAsync(CreateTeamRequest request, Guid userId)
        {
            try
            {
                var currentUserRole = await GetRoleByUserId(userId);
                var currentUserPermissions = await GetPermissionsByUserId(userId);
                if ((!_rolePermissionConfig.RoleHasPermission(currentUserRole, "CREATE_TEAM")) &&
                    (!_rolePermissionConfig.HasCustomPermission(currentUserPermissions, "CREATE_TEAM")))
                    throw new UnauthorizedAccessException("Bạn không có quyền tạo team.");

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
                    CreatedAt = DateTime.UtcNow
                };

                var createdTeam = await _teamRepository.CreateAsync(team);

                try
                {
                    // Tạo TeamMember cho người tạo team với role Vendor
                    var teamMember = new TeamMember
                    {
                        Id = Guid.NewGuid(),
                        TeamId = createdTeam.Id,
                        UserId = userId,
                        Role = "Vendor",
                        Permissions = new List<string> { },
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

                // Lấy thông tin user để có email
                var user = await _userRepository.GetByIdAsync(userId);
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
                var currentUserRole = await GetRoleByUserId(userId);
                var currentUserPermissions = await GetPermissionsByUserId(userId);
                if ((!_rolePermissionConfig.RoleHasPermission(currentUserRole, "LIST_TEAM_MEMBERS")) &&
                    (!_rolePermissionConfig.HasCustomPermission(currentUserPermissions, "LIST_TEAM_MEMBERS")))
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
                var currentUserRole = await GetRoleByUserId(userId);
                var currentUserPermissions = await GetPermissionsByUserId(userId);
                if ((!_rolePermissionConfig.RoleHasPermission(currentUserRole, "LIST_TEAM_MEMBERS")) &&
                    (!_rolePermissionConfig.HasCustomPermission(currentUserPermissions, "LIST_TEAM_MEMBERS")))
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
                var currentUserRole = await GetRoleByUserId(userId);
                var currentUserPermissions = await GetPermissionsByUserId(userId);
                if ((!_rolePermissionConfig.RoleHasPermission(currentUserRole, "UPDATE_TEAM")) &&
                    (!_rolePermissionConfig.HasCustomPermission(currentUserPermissions, "UPDATE_TEAM")))
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
                var currentUserRole = await GetRoleByUserId(userId);
                var currentUserPermissions = await GetPermissionsByUserId(userId);
                if ((!_rolePermissionConfig.RoleHasPermission(currentUserRole, "DELETE_TEAM")) &&
                    (!_rolePermissionConfig.HasCustomPermission(currentUserPermissions, "DELETE_TEAM")))
                    throw new UnauthorizedAccessException("Bạn không có quyền xóa team.");

                // Lấy thông tin team hiện tại để kiểm tra tồn tại
                var existingTeam = await _teamRepository.GetByIdAsync(id);
                if (existingTeam == null)
                {
                    return GenericResponse<bool>.CreateError("Không tìm thấy team để xóa.");
                }

                // Xóa team từ repository
                await _teamRepository.DeleteAsync(id);

                return GenericResponse<bool>.CreateSuccess(true, "Xóa team thành công");
            }
            catch (Exception ex)
            {
                return GenericResponse<bool>.CreateError($"Lỗi khi xóa team: {ex.Message}");
            }
        }

        
        

        private async Task<string> GetRoleByUserId(Guid userId)
        {
            var member = await _teamMemberRepository.GetByUserIdAsync(userId);
            if (member == null || string.IsNullOrEmpty(member.Role))
                return "Member"; // default role
            return member.Role;
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