using AISAM.Services.IServices;
using AISAM.API.Utils;
using AISAM.Data.Enumeration;

namespace AISAM.API.Middleware
{
    public class SubscriptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _serviceProvider;

        public SubscriptionMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
        {
            _next = next;
            _serviceProvider = serviceProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip middleware for certain endpoints
            var path = context.Request.Path.Value?.ToLower();
            if (path == null ||
                path.StartsWith("/api/auth") ||
                path.StartsWith("/api/payment") ||
                path.StartsWith("/api/user/profile") ||
                path.Contains("swagger"))
            {
                await _next(context);
                return;
            }

            // Check if endpoint requires premium feature
            var featureAttribute = context.GetEndpoint()?.Metadata.GetMetadata<PremiumFeatureAttribute>();
            if (featureAttribute != null)
            {
                var userId = UserClaimsHelper.GetUserIdOrThrow(context.User);
                if (userId == Guid.Empty)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        success = false,
                        message = "Authentication required"
                    });
                    return;
                }

                using var scope = _serviceProvider.CreateScope();
                var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionValidationService>();

                var hasAccess = await subscriptionService.CanUseFeatureAsync(userId, featureAttribute.FeatureName);
                if (!hasAccess)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        success = false,
                        message = $"Premium feature required: {featureAttribute.FeatureName}",
                        requiredPlan = featureAttribute.RequiredPlan.ToString()
                    });
                    return;
                }
            }

            await _next(context);
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class PremiumFeatureAttribute : Attribute
    {
        public string FeatureName { get; }
        public SubscriptionPlanEnum RequiredPlan { get; }

        public PremiumFeatureAttribute(string featureName, SubscriptionPlanEnum requiredPlan = SubscriptionPlanEnum.Basic)
        {
            FeatureName = featureName;
            RequiredPlan = requiredPlan;
        }
    }
}