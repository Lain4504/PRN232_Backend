using System.Text.Json;

namespace AISAM.Services.Config
{
    public class RolePermissionConfig
    {
        private readonly Dictionary<string, List<string>> _rolePermissions = new();
        private readonly Dictionary<string, string> _permissionDescriptions = new();

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public RolePermissionConfig()
        {
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

            Console.WriteLine($"[DEBUG] Loaded roles: {_rolePermissions.Count}");
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

            Console.WriteLine($"[DEBUG] Loaded {_permissionDescriptions.Count} permission descriptions.");
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
                Console.WriteLine($"[DEBUG] Role '{role}' has permission '{permission}': {hasPermission}");
                return hasPermission;
            }

            Console.WriteLine($"[DEBUG] Role '{role}' not found in _rolePermissions.");
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
