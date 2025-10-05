using System.Security.Claims;

namespace AISAM.API.Middleware
{
    public class AuthorizationLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthorizationLoggingMiddleware> _logger;

        public AuthorizationLoggingMiddleware(RequestDelegate next, ILogger<AuthorizationLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Log request details
            var method = context.Request.Method;
            var path = context.Request.Path;
            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = context.User?.FindFirst(ClaimTypes.Role)?.Value;
            var isAuthenticated = context.User?.Identity?.IsAuthenticated ?? false;

            _logger.LogInformation("Request started: {Method} {Path}, IsAuthenticated: {IsAuthenticated}, UserId: {UserId}, Role: {UserRole}", 
                method, path, isAuthenticated, userId ?? "null", userRole ?? "null");

            // Log all claims for debugging
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var claims = context.User.Claims.Select(c => $"{c.Type}={c.Value}").ToList();
                _logger.LogDebug("User claims: {Claims}", string.Join(", ", claims));
            }

            await _next(context);

            // Log response status
            var statusCode = context.Response.StatusCode;
            
            if (statusCode == 401)
            {
                _logger.LogWarning("Request resulted in 401 Unauthorized: {Method} {Path}, IsAuthenticated: {IsAuthenticated}, UserId: {UserId}", 
                    method, path, isAuthenticated, userId ?? "null");
                
                if (!isAuthenticated)
                {
                    _logger.LogWarning("401 Reason: User is not authenticated. Missing or invalid JWT token.");
                }
                else
                {
                    _logger.LogWarning("401 Reason: User is authenticated but token may be expired or invalid.");
                }
            }
            else if (statusCode == 403)
            {
                _logger.LogWarning("Request resulted in 403 Forbidden: {Method} {Path}, UserId: {UserId}, Role: {UserRole}", 
                    method, path, userId ?? "null", userRole ?? "null");
                _logger.LogWarning("403 Reason: User is authenticated but does not have permission to access this resource.");
            }

            _logger.LogInformation("Request completed: {Method} {Path}, StatusCode: {StatusCode}, UserId: {UserId}", 
                method, path, statusCode, userId ?? "null");
        }
    }
}