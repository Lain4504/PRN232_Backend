using AISAM.Common.Dtos.Request;
using AISAM.Data.Enumeration;
using FluentValidation;

namespace AISAM.API.Validators
{
    public class UpdateApprovalRequestValidator : AbstractValidator<UpdateApprovalRequest>
    {
        public UpdateApprovalRequestValidator()
        {
            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Status must be a valid ContentStatusEnum value");

            RuleFor(x => x.Status)
                .Must(status => status == ContentStatusEnum.Approved || 
                               status == ContentStatusEnum.Rejected || 
                               status == ContentStatusEnum.PendingApproval)
                .WithMessage("Status must be Approved, Rejected, or PendingApproval");

            RuleFor(x => x.Notes)
                .MaximumLength(1000)
                .WithMessage("Notes cannot exceed 1000 characters");
        }
    }
}