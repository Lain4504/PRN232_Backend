using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AISAM.Services.Helper
{
    public class RolePermissionConfig
    {
        private readonly Dictionary<string, List<string>> _rolePermissions = new();
        private readonly Dictionary<string, string> _permissionDescriptions = new();
        private readonly ILogger<RolePermissionConfig> _logger;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public RolePermissionConfig(ILogger<RolePermissionConfig> logger)
        {
            _logger = logger;
            LoadRoles();
            LoadPermissions();
        }

        private void LoadRoles()
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "Config", "team_roles.json");
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Không tìm thấy file team_roles.json", filePath);

            var json = File.ReadAllText(filePath);
            var rawRoles = JsonSerializer.Deserialize<Dictionary<string, RoleDefinition>>(json, _jsonOptions);

            if (rawRoles == null)
                throw new InvalidOperationException("Không thể đọc dữ liệu role từ team_roles.json");

            foreach (var kv in rawRoles)
            {
                var roleKey = kv.Key?.Trim();
                if (!string.IsNullOrEmpty(roleKey))
                {
                    _rolePermissions[roleKey] = kv.Value.Permissions?.Select(p => p.Trim()).ToList() ?? new List<string>();
                    _permissionDescriptions[roleKey] = kv.Value.Description?.Trim() ?? string.Empty;
                }
            }

            _logger.LogInformation("Loaded {Count} roles from team_roles.json", _rolePermissions.Count);
        }

        private void LoadPermissions()
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "Config", "team_permissions.json");
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Không tìm thấy file permission config: {filePath}");

            var json = File.ReadAllText(filePath);
            var permissionData = JsonSerializer.Deserialize<Dictionary<string, string>>(json, _jsonOptions);

            if (permissionData == null || permissionData.Count == 0)
                throw new InvalidOperationException("File permission config rỗng hoặc không hợp lệ.");

            foreach (var kvp in permissionData)
            {
                var key = kvp.Key?.Trim();
                var desc = kvp.Value?.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(key))
                    _permissionDescriptions[key] = desc;
            }

            _logger.LogInformation("Loaded {Count} permission descriptions from team_permissions.json", _permissionDescriptions.Count);
        }

        public bool HasCustomPermission(List<string> customPermissions, string permission)
        {
            if (customPermissions == null || !customPermissions.Any() || string.IsNullOrEmpty(permission))
            {
                return false;
            }

            return customPermissions.Any(p => string.Equals(p, permission.Trim(), StringComparison.OrdinalIgnoreCase));
        }
        public bool RoleHasPermission(string role, string permission)
        {
            if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(permission))
                return false;

            role = role.Trim();
            permission = permission.Trim();

            if (_rolePermissions.TryGetValue(role, out var permissions))
            {
                var hasPermission = permissions.Any(p => string.Equals(p, permission, StringComparison.OrdinalIgnoreCase));
                _logger.LogDebug("Role '{Role}' has permission '{Permission}': {HasPermission}", role, permission, hasPermission);
                return hasPermission;
            }

            _logger.LogWarning("Role '{Role}' not found in role permissions configuration", role);
            return false;
        }

        public List<string> GetPermissionsByRole(string role)
        {
            if (string.IsNullOrEmpty(role)) return new List<string>();
            return _rolePermissions.TryGetValue(role.Trim(), out var permissions) ? permissions : new List<string>();
        }

        public Dictionary<string, List<string>> GetAllRolePermissions() => _rolePermissions;

        public Dictionary<string, string> GetAllPermissionDescriptions() => _permissionDescriptions;

        private class RoleDefinition
        {
            public string Description { get; set; } = string.Empty;
            public List<string> Permissions { get; set; } = new();
        }
    }
}
