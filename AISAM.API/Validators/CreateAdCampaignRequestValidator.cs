using AISAM.Common.Dtos.Request;
using FluentValidation;

namespace AISAM.API.Validators
{
    public class CreateAdCampaignRequestValidator : AbstractValidator<CreateAdCampaignRequest>
    {
        private static readonly string[] ValidObjectives = 
        {
            "REACH", "IMPRESSIONS", "BRAND_AWARENESS", "TRAFFIC", "ENGAGEMENT",
            "APP_INSTALLS", "VIDEO_VIEWS", "LEAD_GENERATION", "CONVERSIONS",
            "CATALOG_SALES", "STORE_TRAFFIC", "EVENT_RESPONSES", "MESSAGES"
        };

        public CreateAdCampaignRequestValidator()
        {
            RuleFor(x => x.BrandId)
                .NotEmpty()
                .WithMessage("Brand ID is required");

            RuleFor(x => x.AdAccountId)
                .NotEmpty()
                .WithMessage("Ad Account ID is required")
                .MaximumLength(255)
                .WithMessage("Ad Account ID cannot exceed 255 characters");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Campaign name is required")
                .MaximumLength(255)
                .WithMessage("Campaign name cannot exceed 255 characters");

            RuleFor(x => x.Objective)
                .NotEmpty()
                .WithMessage("Campaign objective is required")
                .Must(BeValidObjective)
                .WithMessage($"Objective must be one of: {string.Join(", ", ValidObjectives)}");

            RuleFor(x => x.Budget)
                .GreaterThan(0)
                .WithMessage("Budget must be greater than 0")
                .LessThanOrEqualTo(1000000)
                .WithMessage("Budget cannot exceed $1,000,000");

            RuleFor(x => x.StartDate)
                .GreaterThanOrEqualTo(DateTime.Today)
                .WithMessage("Start date cannot be in the past")
                .When(x => x.StartDate.HasValue);

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate)
                .WithMessage("End date must be after start date")
                .When(x => x.EndDate.HasValue && x.StartDate.HasValue);
        }

        private static bool BeValidObjective(string objective)
        {
            return ValidObjectives.Contains(objective.ToUpper());
        }
    }
}
