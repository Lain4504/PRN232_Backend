using AISAM.Common.Dtos.Request;
using FluentValidation;

namespace AISAM.API.Validators
{
    public class UpdatePostRequestValidator : AbstractValidator<UpdatePostRequest>
    {
        public UpdatePostRequestValidator()
        {
            RuleFor(x => x.ContentId)
                .NotEmpty()
                .WithMessage("ContentId là bắt buộc");

            RuleFor(x => x.IntegrationId)
                .NotEmpty()
                .WithMessage("IntegrationId là bắt buộc");

            RuleFor(x => x.ExternalPostId)
                .MaximumLength(255)
                .WithMessage("ExternalPostId không được vượt quá 255 ký tự");

            RuleFor(x => x.PublishedAt)
                .NotEmpty()
                .WithMessage("PublishedAt là bắt buộc")
                .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
                .WithMessage("PublishedAt không được vượt quá 5 phút trong tương lai");
        }
    }
}