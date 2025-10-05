using AISAM.Data.Enumeration;
using AISAM.Data.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System.Security.Claims;

namespace AISAM.API.Authorization
{
    /// <summary>
    /// Authorization handler for Approval operations based on brand ownership
    /// </summary>
    public class ApprovalAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, Approval>
    {
        private readonly ILogger<ApprovalAuthorizationHandler> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ApprovalAuthorizationHandler(ILogger<ApprovalAuthorizationHandler> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OperationAuthorizationRequirement requirement,
            Approval resource)
        {
            var operation = requirement.Name;
            var approvalId = resource?.Id.ToString() ?? "null";

            _logger.LogInformation("Authorization check started for Approval {ApprovalId}, Operation: {Operation}",
                approvalId, operation);

            if (context.User == null)
            {
                _logger.LogWarning("Authorization failed for Approval {ApprovalId}: User context is null", approvalId);
                return;
            }

            if (resource == null)
            {
                _logger.LogWarning("Authorization failed for Operation {Operation}: Approval resource is null", operation);
                return;
            }

            // Get user ID from claims
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Authorization failed for Approval {ApprovalId}: Invalid or missing user ID claim. Claim value: {UserIdClaim}",
                    approvalId, userIdClaim ?? "null");
                return;
            }

            // Get user role from database and brand info
            using var scope = _serviceProvider.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<AISAM.Repositories.IRepositories.IUserRepository>();

            var user = await userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Authorization failed for Approval {ApprovalId}: User {UserId} not found in database",
                    approvalId, userId);
                return;
            }

            var userRole = user.Role;

            _logger.LogInformation("Authorization check for Approval {ApprovalId}: UserId={UserId}, Role={UserRole}, Operation={Operation}",
                approvalId, userId, userRole, operation);

            // Admin can do everything
            if (userRole == UserRoleEnum.Admin)
            {
                _logger.LogInformation("Authorization SUCCESS for Approval {ApprovalId}: User {UserId} is Admin",
                    approvalId, userId);
                context.Succeed(requirement);
                return;
            }

            // For regular users, check brand ownership through content
            if (userRole == UserRoleEnum.User)
            {
                if (resource.Content == null)
                {
                    _logger.LogWarning("Authorization failed for Approval {ApprovalId}: Content is null. UserId: {UserId}",
                        approvalId, userId);
                    return;
                }

                var contentId = resource.Content.Id;
                var brandId = resource.Content.BrandId;
                var approverId = resource.ApproverId;

                // Check if Brand navigation property is loaded
                if (resource.Content.Brand == null)
                {
                    _logger.LogWarning("Authorization failed for Approval {ApprovalId}: Content.Brand navigation property is null. ContentId: {ContentId}, BrandId: {BrandId}, UserId: {UserId}",
                        approvalId, contentId, brandId, userId);
                    return;
                }

                var brandOwnerId = resource.Content.Brand.UserId;

                _logger.LogInformation("Authorization details for Approval {ApprovalId}: ContentId={ContentId}, BrandId={BrandId}, BrandOwnerId={BrandOwnerId}, ApproverId={ApproverId}",
                    approvalId, contentId, brandId, brandOwnerId, approverId);

                // User can only access approvals for content of brands they own
                if (resource.Content.Brand.UserId == userId)
                {
                    _logger.LogInformation("Authorization SUCCESS for Approval {ApprovalId}: User {UserId} owns the brand {BrandId}",
                        approvalId, userId, brandId);
                    context.Succeed(requirement);
                    return;
                }

                // Users can also read/approve/reject if they are the assigned approver
                var isApprovalOperation = operation == Operations.Approve.Name ||
                                        operation == Operations.Reject.Name ||
                                        operation == Operations.Update.Name ||
                                        operation == Operations.Read.Name;

                if (isApprovalOperation && approverId == userId)
                {
                    _logger.LogInformation("Authorization SUCCESS for Approval {ApprovalId}: User {UserId} is the assigned approver for operation {Operation}",
                        approvalId, userId, operation);
                    context.Succeed(requirement);
                    return;
                }

                // Log the specific reason for failure
                if (resource.Content.Brand.UserId != userId)
                {
                    _logger.LogWarning("Authorization FAILED for Approval {ApprovalId}: User {UserId} does not own brand {BrandId} (owned by {BrandOwnerId}). Operation: {Operation}",
                        approvalId, userId, brandId, brandOwnerId, operation);
                }

                if (operation == Operations.Read.Name && resource.ApproverId != userId)
                {
                    _logger.LogWarning("Authorization FAILED for Approval {ApprovalId}: User {UserId} is not the assigned approver {ApproverId} and does not own the brand for Read operation",
                        approvalId, userId, approverId);
                }
                else if ((operation == Operations.Approve.Name || operation == Operations.Reject.Name || operation == Operations.Update.Name) && resource.ApproverId != userId)
                {
                    _logger.LogWarning("Authorization FAILED for Approval {ApprovalId}: User {UserId} is not the assigned approver {ApproverId} for operation {Operation}",
                        approvalId, userId, approverId, operation);
                }
            }
            else
            {
                _logger.LogWarning("Authorization FAILED for Approval {ApprovalId}: Unsupported user role {UserRole}. UserId: {UserId}, Operation: {Operation}",
                    approvalId, userRole, userId, operation);
            }

            _logger.LogWarning("Authorization FAILED for Approval {ApprovalId}: No authorization rule matched. UserId: {UserId}, Role: {UserRole}, Operation: {Operation}",
                approvalId, userId, userRole, operation);
        }
    }


}