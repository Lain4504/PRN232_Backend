using Microsoft.AspNetCore.Authorization;

namespace AISAM.API.Authorization
{
    /// <summary>
    /// Requirement for brand ownership - User can only access resources of brands they own
    /// </summary>
    public class BrandOwnershipRequirement : IAuthorizationRequirement
    {
    }

    /// <summary>
    /// Requirement for admin access - Admin can access all resources
    /// </summary>
    public class AdminAccessRequirement : IAuthorizationRequirement
    {
    }
}