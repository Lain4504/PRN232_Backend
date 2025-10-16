using AISAM.Common.Dtos.Request;
using FluentValidation;

namespace AISAM.API.Validators
{
    public class CreateAdCreativeRequestValidator : AbstractValidator<CreateAdCreativeRequest>
    {
        private static readonly string[] ValidCallToActions = 
        {
            "SHOP_NOW", "LEARN_MORE", "SIGN_UP", "DOWNLOAD", "BOOK_TRAVEL",
            "GET_QUOTE", "CONTACT_US", "DONATE", "APPLY_NOW", "GET_OFFER"
        };

        public CreateAdCreativeRequestValidator()
        {
            RuleFor(x => x.ContentId)
                .NotEmpty()
                .WithMessage("Content ID is required");

            RuleFor(x => x.AdAccountId)
                .NotEmpty()
                .WithMessage("Ad Account ID is required")
                .MaximumLength(255)
                .WithMessage("Ad Account ID cannot exceed 255 characters");

            RuleFor(x => x.CallToAction)
                .Must(BeValidCallToAction)
                .WithMessage($"Call to action must be one of: {string.Join(", ", ValidCallToActions)}")
                .When(x => !string.IsNullOrEmpty(x.CallToAction));
        }

        private static bool BeValidCallToAction(string? callToAction)
        {
            if (string.IsNullOrEmpty(callToAction))
                return true; // Optional field

            return ValidCallToActions.Contains(callToAction.ToUpper());
        }
    }
}
