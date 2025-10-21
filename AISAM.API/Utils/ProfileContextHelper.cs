using System.Security.Claims;
using AISAM.Data.Enumeration;

namespace AISAM.API.Utils
{
    public static class ProfileContextHelper
    {
        /// <summary>
        /// Get the active profile ID from the X-Profile-Id header
        /// </summary>
        public static bool TryGetActiveProfileId(HttpContext? httpContext, out Guid profileId)
        {
            profileId = Guid.Empty;
            if (httpContext == null)
            {
                return false;
            }

            var profileIdHeader = httpContext.Request.Headers["X-Profile-Id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(profileIdHeader))
            {
                return false;
            }

            return Guid.TryParse(profileIdHeader, out profileId);
        }

        /// <summary>
        /// Get the active profile ID from the X-Profile-Id header or throw exception
        /// </summary>
        public static Guid GetActiveProfileIdOrThrow(HttpContext? httpContext)
        {
            if (TryGetActiveProfileId(httpContext, out var profileId))
            {
                return profileId;
            }

            throw new UnauthorizedAccessException("Invalid profile context: missing or invalid X-Profile-Id header.");
        }

        /// <summary>
        /// Validate that the user owns the specified profile
        /// </summary>
        public static bool ValidateProfileAccess(ClaimsPrincipal? user, Guid profileUserId)
        {
            if (user == null)
            {
                return false;
            }

            if (!UserClaimsHelper.TryGetUserId(user, out var userId))
            {
                return false;
            }

            return userId == profileUserId;
        }

        /// <summary>
        /// Check if the profile type supports the specified feature
        /// </summary>
        public static bool CheckProfileFeatureAccess(ProfileTypeEnum profileType, string feature)
        {
            return feature.ToLower() switch
            {
                "teams" => profileType != ProfileTypeEnum.Free, // Only Basic and Pro can use teams
                "advanced_analytics" => profileType == ProfileTypeEnum.Pro, // Only Pro gets advanced analytics
                "priority_support" => profileType == ProfileTypeEnum.Pro, // Only Pro gets priority support
                _ => true // Default to allowing access for unknown features
            };
        }

        /// <summary>
        /// Get feature access error message for profile type
        /// </summary>
        public static string GetFeatureAccessErrorMessage(ProfileTypeEnum profileType, string feature)
        {
            return feature.ToLower() switch
            {
                "teams" => "Team features require Basic or Pro subscription. Please upgrade your profile.",
                "advanced_analytics" => "Advanced analytics require Pro subscription. Please upgrade your profile.",
                "priority_support" => "Priority support requires Pro subscription. Please upgrade your profile.",
                _ => $"This feature is not available for {profileType} profiles."
            };
        }
    }
}
