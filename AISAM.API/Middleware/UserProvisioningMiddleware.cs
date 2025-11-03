using AISAM.API.Utils;
using AISAM.Services.IServices;

namespace AISAM.API.Middleware
{
    /// <summary>
    /// Middleware to automatically provision user in database if they don't exist.
    /// This runs after authentication and checks if the authenticated user exists in our database.
    /// If not, creates the user from JWT claims.
    /// </summary>
    public class UserProvisioningMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<UserProvisioningMiddleware> _logger;

        public UserProvisioningMiddleware(RequestDelegate next, ILogger<UserProvisioningMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IUserService userService)
        {
            // Only process authenticated requests
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                try
                {
                    // Try to get user ID and email from JWT claims
                    if (UserClaimsHelper.TryGetUserId(context.User, out var userId) &&
                        UserClaimsHelper.TryGetEmail(context.User, out var email) &&
                        !string.IsNullOrWhiteSpace(email))
                    {
                        // Check if user exists in our database
                        var existingUser = await userService.GetUserByIdAsync(userId);
                        
                        if (existingUser == null)
                        {
                            // User doesn't exist, create them from JWT claims
                            try
                            {
                                await userService.CreateUserAsync(userId, email);
                                _logger.LogInformation("Auto-provisioned user {UserId} with email {Email} from JWT", userId, email);
                            }
                            catch (InvalidOperationException)
                            {
                                // User was created by another concurrent request, that's fine
                                _logger.LogDebug("User {UserId} already exists (created concurrently)", userId);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't block the request
                    // User provisioning is best-effort, authentication is already validated
                    _logger.LogWarning(ex, "Failed to provision user from JWT claims");
                }
            }

            // Continue to next middleware
            await _next(context);
        }
    }

    // Extension method to easily add this middleware to the pipeline
    public static class UserProvisioningMiddlewareExtensions
    {
        public static IApplicationBuilder UseUserProvisioning(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<UserProvisioningMiddleware>();
        }
    }
}

