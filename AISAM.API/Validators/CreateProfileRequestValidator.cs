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
                .WithMessage("Loại hồ sơ phải là Cá nhân hoặc Doanh nghiệp");

            RuleFor(x => x.CompanyName)
                .NotEmpty()
                .WithMessage("Tên công ty là bắt buộc đối với hồ sơ doanh nghiệp")
                .When(x => x.ProfileType == ProfileTypeEnum.Business);

            RuleFor(x => x.CompanyName)
                .Empty()
                .WithMessage("Hồ sơ cá nhân không được có tên công ty")
                .When(x => x.ProfileType == ProfileTypeEnum.Personal);

            RuleFor(x => x.CompanyName)
                .MaximumLength(255)
                .WithMessage("Tên công ty không được vượt quá 255 ký tự")
                .When(x => !string.IsNullOrEmpty(x.CompanyName));

            RuleFor(x => x.Bio)
                .MaximumLength(1000)
                .WithMessage("Tiểu sử không được vượt quá 1000 ký tự")
                .When(x => !string.IsNullOrEmpty(x.Bio));

            RuleFor(x => x.AvatarUrl)
                .MaximumLength(500)
                .WithMessage("URL ảnh đại diện không được vượt quá 500 ký tự")
                .When(x => !string.IsNullOrEmpty(x.AvatarUrl));
        }
    }
}