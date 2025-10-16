using AISAM.Common.Dtos.Request;
using FluentValidation;
using System.Text.Json;

namespace AISAM.API.Validators
{
    public class CreateAdSetRequestValidator : AbstractValidator<CreateAdSetRequest>
    {
        public CreateAdSetRequestValidator()
        {
            RuleFor(x => x.CampaignId)
                .NotEmpty()
                .WithMessage("Campaign ID is required");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Ad set name is required")
                .MaximumLength(255)
                .WithMessage("Ad set name cannot exceed 255 characters");

            RuleFor(x => x.Targeting)
                .NotEmpty()
                .WithMessage("Targeting configuration is required")
                .Must(BeValidTargetingJson)
                .WithMessage("Targeting must be valid JSON");

            RuleFor(x => x.DailyBudget)
                .GreaterThan(0)
                .WithMessage("Daily budget must be greater than 0")
                .LessThanOrEqualTo(100000)
                .WithMessage("Daily budget cannot exceed $100,000");

            RuleFor(x => x.StartDate)
                .GreaterThanOrEqualTo(DateTime.Today)
                .WithMessage("Start date cannot be in the past")
                .When(x => x.StartDate.HasValue);

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate)
                .WithMessage("End date must be after start date")
                .When(x => x.EndDate.HasValue && x.StartDate.HasValue);
        }

        private static bool BeValidTargetingJson(string targeting)
        {
            if (string.IsNullOrWhiteSpace(targeting))
                return false;

            try
            {
                JsonDocument.Parse(targeting);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
