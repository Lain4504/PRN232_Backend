using AISAM.Common.Models;
using FluentValidation;

namespace AISAM.API.Validators
{
    public class CreatePostRequestValidator : AbstractValidator<CreatePostRequest>
    {
        public CreatePostRequestValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId không được để trống");

            RuleFor(x => x.SocialAccountId)
                .NotEmpty().WithMessage("SocialAccountId không được để trống");

            RuleFor(x => x.SocialTargetId)
                .NotEmpty().WithMessage("SocialTargetId không được để trống");

            RuleFor(x => x.Message)
                .NotEmpty().WithMessage("Message không được để trống")
                .MaximumLength(5000).WithMessage("Message tối đa 5000 ký tự");

            RuleFor(x => x.LinkUrl)
                .Must(BeValidUrlOrNull).WithMessage("LinkUrl không hợp lệ");

            RuleFor(x => x.ImageUrl)
                .Must(BeValidUrlOrNull).WithMessage("ImageUrl không hợp lệ");
        }

        private bool BeValidUrlOrNull(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return true;
            }

            return Uri.TryCreate(url, UriKind.Absolute, out var _);
        }
    }
}


