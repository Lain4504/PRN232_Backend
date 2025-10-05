using AISAM.Common.Dtos.Request;
using FluentValidation;

namespace AISAM.API.Validators
{
    public class CreateApprovalRequestValidator : AbstractValidator<CreateApprovalRequest>
    {
        public CreateApprovalRequestValidator()
        {
            RuleFor(x => x.ContentId)
                .NotEmpty()
                .WithMessage("ContentId is required");

            RuleFor(x => x.ApproverId)
                .NotEmpty()
                .WithMessage("ApproverId is required");

            RuleFor(x => x.Notes)
                .MaximumLength(1000)
                .WithMessage("Notes cannot exceed 1000 characters");
        }
    }
}