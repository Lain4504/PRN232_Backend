using AISAM.Common.Dtos.Request;
using FluentValidation;

namespace AISAM.API.Validators
{
    public class PublishAdRequestValidator : AbstractValidator<PublishAdRequest>
    {
        public PublishAdRequestValidator()
        {
            RuleFor(x => x.AdSetId).NotEmpty();
            RuleFor(x => x.CreativeId).NotEmpty();
        }
    }
}


