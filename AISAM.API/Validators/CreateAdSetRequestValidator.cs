using AISAM.Common.Dtos.Request;
using FluentValidation;

namespace AISAM.API.Validators
{
    public class CreateAdSetRequestValidator : AbstractValidator<CreateAdSetRequest>
    {
        public CreateAdSetRequestValidator()
        {
            RuleFor(x => x.CampaignId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
            RuleFor(x => x.DailyBudget).GreaterThan(0).When(x => x.DailyBudget.HasValue);
            RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).When(x => x.EndDate.HasValue && x.StartDate.HasValue);
        }
    }
}


