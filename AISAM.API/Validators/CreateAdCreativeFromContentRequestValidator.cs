using FluentValidation;
using System.Text.Json;
using AISAM.Common.Dtos.Request;

namespace AISAM.API.Validators
{
    public class CreateAdCreativeFromContentRequestValidator : AbstractValidator<CreateAdCreativeFromContentRequest>
    {
        public CreateAdCreativeFromContentRequestValidator()
        {
            RuleFor(x => x.ContentId)
                .NotEmpty()
                .WithMessage("Content ID is required");

            RuleFor(x => x.AdAccountId)
                .NotEmpty()
                .WithMessage("Ad account ID is required")
                .MaximumLength(255)
                .WithMessage("Ad account ID cannot exceed 255 characters");

            RuleFor(x => x.CallToAction)
                .MaximumLength(50)
                .WithMessage("Call to action cannot exceed 50 characters")
                .Must(BeValidCallToAction)
                .When(x => !string.IsNullOrEmpty(x.CallToAction))
                .WithMessage("Invalid call to action. Valid options: SHOP_NOW, LEARN_MORE, SIGN_UP, DOWNLOAD, BOOK_TRAVEL, GET_QUOTE");

            RuleFor(x => x.LinkUrl)
                .MaximumLength(500)
                .WithMessage("Link URL cannot exceed 500 characters")
                .Must(BeValidUrl)
                .When(x => !string.IsNullOrEmpty(x.LinkUrl))
                .WithMessage("Link URL must be a valid URL");

            RuleFor(x => x.AdName)
                .MaximumLength(255)
                .WithMessage("Ad name cannot exceed 255 characters");
        }

        private bool BeValidCallToAction(string? callToAction)
        {
            if (string.IsNullOrEmpty(callToAction))
                return true;

            var validCallToActions = new[] { "SHOP_NOW", "LEARN_MORE", "SIGN_UP", "DOWNLOAD", "BOOK_TRAVEL", "GET_QUOTE" };
            return validCallToActions.Contains(callToAction.ToUpper());
        }

        private bool BeValidUrl(string? url)
        {
            if (string.IsNullOrEmpty(url))
                return true;

            return Uri.TryCreate(url, UriKind.Absolute, out var result) && 
                   (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
        }
    }
}
