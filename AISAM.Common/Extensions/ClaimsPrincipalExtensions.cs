using AISAM.Data.Enumeration;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace AISAM.Common.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid? GetUserId(this ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        public static UserRoleEnum? GetUserRole(this ClaimsPrincipal user)
        {
            var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
            return Enum.TryParse<UserRoleEnum>(roleClaim, out var role) ? role : null;
        }

        public static bool IsAdmin(this ClaimsPrincipal user)
        {
            return user.GetUserRole() == UserRoleEnum.Admin;
        }

        public static bool IsUser(this ClaimsPrincipal user)
        {
            return user.GetUserRole() == UserRoleEnum.User;
        }

        public static bool IsVendor(this ClaimsPrincipal user)
        {
            return user.GetUserRole() == UserRoleEnum.Vendor;
        }

        public static string GetAuthorizationSummary(this ClaimsPrincipal user)
        {
            var userId = user.GetUserId()?.ToString() ?? "null";
            var role = user.GetUserRole()?.ToString() ?? "null";
            var isAuthenticated = user.Identity?.IsAuthenticated ?? false;
            var claims = user.Claims.Select(c => $"{c.Type}={c.Value}").ToList();

            return $"UserId: {userId}, Role: {role}, IsAuthenticated: {isAuthenticated}, Claims: [{string.Join(", ", claims)}]";
        }

        public static void LogAuthorizationDetails(this ClaimsPrincipal user, ILogger logger, string operation, string resource)
        {
            logger.LogInformation("Authorization Details - Operation: {Operation}, Resource: {Resource}, {AuthSummary}",
                operation, resource, user.GetAuthorizationSummary());
        }
    }
}