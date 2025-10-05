using AISAM.Common.Dtos.Request;
using AISAM.Data.Enumeration;
using FluentValidation;

namespace AISAM.API.Validators
{
    public class CreateProfileRequestValidator : AbstractValidator<CreateProfileRequest>
    {
        public CreateProfileRequestValidator()
        {
            RuleFor(x => x.ProfileType)
                .IsInEnum()
                .WithMessage("ProfileType must be either Personal or Business");

            RuleFor(x => x.CompanyName)
                .NotEmpty()
                .WithMessage("Company name is required for business profiles")
                .When(x => x.ProfileType == ProfileTypeEnum.Business);

            RuleFor(x => x.CompanyName)
                .MaximumLength(255)
                .WithMessage("Company name must not exceed 255 characters")
                .When(x => !string.IsNullOrEmpty(x.CompanyName));

            RuleFor(x => x.Bio)
                .MaximumLength(1000)
                .WithMessage("Bio must not exceed 1000 characters")
                .When(x => !string.IsNullOrEmpty(x.Bio));

            RuleFor(x => x.AvatarUrl)
                .Must(BeValidUrl)
                .WithMessage("Avatar URL must be a valid URL")
                .When(x => !string.IsNullOrEmpty(x.AvatarUrl));

            RuleFor(x => x.AvatarUrl)
                .MaximumLength(500)
                .WithMessage("Avatar URL must not exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.AvatarUrl));
        }

        private bool BeValidUrl(string? url)
        {
            if (string.IsNullOrEmpty(url))
                return true;

            return Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}