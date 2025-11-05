using System.Security.Claims;
using AISAM.Data.Enumeration;
using AISAM.Services.IServices;

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

        /// <summary>
        /// Check if current user is admin by querying role from database.
        /// Note: Supabase JWT token does NOT contain system roles. Role is stored in our database
        /// and must be queried from the User entity. This method queries the database to get the user's role.
        /// </summary>
        /// <param name="user">ClaimsPrincipal from JWT token (contains userId/sub claim)</param>
        /// <param name="userService">UserService to query user from database</param>
        /// <returns>True if user exists and has Admin role, false otherwise</returns>
        public static async Task<bool> IsAdminAsync(ClaimsPrincipal? user, IUserService userService)
        {
            // Get userId from JWT token (Supabase provides this in 'sub' claim)
            if (!TryGetUserId(user, out var userId))
            {
                return false;
            }

            try
            {
                // Query user from database to get role (Supabase JWT does not contain role)
                var userEntity = await userService.GetUserByIdAsync(userId);
                
                // Check if user exists and has Admin role
                return userEntity != null && userEntity.Role == UserRoleEnum.Admin;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if current user is admin and throw UnauthorizedAccessException if not.
        /// Note: Supabase JWT token does NOT contain system roles. Role is stored in our database
        /// and must be queried from the User entity. This method queries the database to verify admin role.
        /// </summary>
        /// <param name="user">ClaimsPrincipal from JWT token (contains userId/sub claim)</param>
        /// <param name="userService">UserService to query user from database</param>
        /// <exception cref="UnauthorizedAccessException">Thrown if user is not admin</exception>
        public static async Task EnsureAdminAsync(ClaimsPrincipal? user, IUserService userService)
        {
            if (!await IsAdminAsync(user, userService))
            {
                throw new UnauthorizedAccessException("Admin privileges required for this operation");
            }
        }
    }
}