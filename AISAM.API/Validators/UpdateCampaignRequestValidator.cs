using AISAM.Common.Dtos.Request;
using FluentValidation;

namespace AISAM.API.Validators
{
    public class UpdateCampaignRequestValidator : AbstractValidator<UpdateCampaignRequest>
    {
        public UpdateCampaignRequestValidator()
        {
            RuleFor(x => x.Name).MaximumLength(255).When(x => x.Name != null);
            RuleFor(x => x.Objective).MaximumLength(100).When(x => x.Objective != null);
            RuleFor(x => x.Budget).GreaterThan(0).When(x => x.Budget.HasValue);
        }
    }
}


