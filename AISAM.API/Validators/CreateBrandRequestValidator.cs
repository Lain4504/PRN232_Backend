using AISAM.Common.Dtos.Request;
using FluentValidation;

namespace AISAM.API.Validators
{
    public class CreateBrandRequestValidator : AbstractValidator<CreateBrandRequest>
    {
        public CreateBrandRequestValidator()
        {
            // Name bắt buộc, max 255 ký tự
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name là bắt buộc")
                .MaximumLength(255).WithMessage("Name tối đa 255 ký tự");

            // Description tối đa 2000 ký tự
            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description tối đa 2000 ký tự")
                .When(x => !string.IsNullOrWhiteSpace(x.Description));

            // LogoUrl tối đa 500 ký tự
            RuleFor(x => x.LogoUrl)
                .MaximumLength(500).WithMessage("LogoUrl tối đa 500 ký tự")
                .When(x => !string.IsNullOrWhiteSpace(x.LogoUrl));

            // Slogan tối đa 255 ký tự
            RuleFor(x => x.Slogan)
                .MaximumLength(255).WithMessage("Slogan tối đa 255 ký tự")
                .When(x => !string.IsNullOrWhiteSpace(x.Slogan));
        }
    }
}