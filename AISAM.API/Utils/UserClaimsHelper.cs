using System.Security.Claims;

namespace AISAM.API.Utils
{
    public static class UserClaimsHelper
    {
        public static bool TryGetUserId(ClaimsPrincipal? user, out Guid userId)
        {
            userId = Guid.Empty;
            if (user == null)
            {
                return false;
            }

            var rawId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? user.FindFirst("sub")?.Value;

            return Guid.TryParse(rawId, out userId);
        }

        public static Guid GetUserIdOrThrow(ClaimsPrincipal? user)
        {
            if (TryGetUserId(user, out var userId))
            {
                return userId;
            }

            throw new UnauthorizedAccessException("Invalid user context: missing or invalid userId (sub).");
        }

        public static string? GetEmail(ClaimsPrincipal? user)
        {
            if (user == null)
            {
                return null;
            }

            // Supabase JWT usually carries "email"; also check standard claim type
            return user.FindFirst(ClaimTypes.Email)?.Value
                   ?? user.FindFirst("email")?.Value;
        }

        public static bool TryGetEmail(ClaimsPrincipal? user, out string? email)
        {
            email = GetEmail(user);
            return !string.IsNullOrWhiteSpace(email);
        }

        public static bool TryGetUserRole(ClaimsPrincipal? user, out string? role)
        {
            role = null;
            if (user == null)
            {
                return false;
            }

            // Check for role in various claim types
            role = user.FindFirst(ClaimTypes.Role)?.Value
                   ?? user.FindFirst("role")?.Value
                   ?? user.FindFirst("user_role")?.Value;

            return !string.IsNullOrWhiteSpace(role);
        }

        public static string GetUserRoleOrThrow(ClaimsPrincipal? user)
        {
            if (TryGetUserRole(user, out var role))
            {
                return role;
            }

            throw new UnauthorizedAccessException("Invalid user context: missing or invalid role.");
        }

        public static bool IsAdmin(ClaimsPrincipal? user)
        {
            return TryGetUserRole(user, out var role) && role?.ToLower() == "admin";
        }
    }
}


