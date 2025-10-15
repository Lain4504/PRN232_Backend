using AISAM.Common.Dtos.Request;
using FluentValidation;

namespace AISAM.API.Validators
{
    public class CreateCreativeRequestValidator : AbstractValidator<CreateCreativeRequest>
    {
        public CreateCreativeRequestValidator()
        {
            RuleFor(x => x.ContentId).NotEmpty();
            RuleFor(x => x.BrandId).NotEmpty();
            RuleFor(x => x.CallToAction).MaximumLength(50).When(x => x.CallToAction != null);
        }
    }
}


