using AISAM.Common.Dtos.Request;
using FluentValidation;

namespace AISAM.API.Validators
{
    public class CreateAdRequestValidator : AbstractValidator<CreateAdRequest>
    {
        private static readonly string[] ValidStatuses = { "ACTIVE", "PAUSED" };

        public CreateAdRequestValidator()
        {
            RuleFor(x => x.AdSetId)
                .NotEmpty()
                .WithMessage("Ad set ID is required");

            RuleFor(x => x.CreativeId)
                .NotEmpty()
                .WithMessage("Creative ID is required");

            RuleFor(x => x.Status)
                .Must(BeValidStatus)
                .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}")
                .When(x => !string.IsNullOrEmpty(x.Status));
        }

        private static bool BeValidStatus(string? status)
        {
            if (string.IsNullOrEmpty(status))
                return true; // Optional field with default value

            return ValidStatuses.Contains(status.ToUpper());
        }
    }
}
