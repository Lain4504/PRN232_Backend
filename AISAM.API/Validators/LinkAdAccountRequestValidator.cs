using AISAM.Common.Dtos.Request;
using FluentValidation;

namespace AISAM.API.Validators
{
    public class LinkAdAccountRequestValidator : AbstractValidator<LinkAdAccountRequest>
    {
        public LinkAdAccountRequestValidator()
        {
            RuleFor(x => x.AdAccountId)
                .NotEmpty()
                .WithMessage("Ad Account ID is required")
                .Matches(@"^act_\d+$")
                .WithMessage("Invalid Facebook Ad Account ID format. Must start with 'act_'");
        }
    }
}