using AISAM.Common.Dtos.Request;
using FluentValidation;

namespace AISAM.API.Validators
{
    public class UpdateAdStatusRequestValidator : AbstractValidator<UpdateAdStatusRequest>
    {
        private static readonly string[] ValidStatuses = { "ACTIVE", "PAUSED" };

        public UpdateAdStatusRequestValidator()
        {
            RuleFor(x => x.AdId)
                .NotEmpty()
                .WithMessage("Ad ID is required");

            RuleFor(x => x.Status)
                .NotEmpty()
                .WithMessage("Status is required")
                .Must(BeValidStatus)
                .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}");
        }

        private static bool BeValidStatus(string status)
        {
            return ValidStatuses.Contains(status.ToUpper());
        }
    }
}
