using AISAM.Common.Models;
using FluentValidation;

namespace AISAM.API.Validators
{
    public class CreatePostRequestValidator : AbstractValidator<CreatePostRequest>
    {
        public CreatePostRequestValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("UserId phải > 0");

            RuleFor(x => x.SocialAccountId)
                .GreaterThan(0).WithMessage("SocialAccountId phải > 0");

            RuleFor(x => x.SocialTargetId)
                .GreaterThan(0).WithMessage("SocialTargetId phải > 0");

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


