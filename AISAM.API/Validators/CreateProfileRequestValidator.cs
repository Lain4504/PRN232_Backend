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

            When(x => x.ProfileType == ProfileTypeEnum.Personal, () =>
            {
                RuleFor(x => x.CompanyName)
                    .Must(x => string.IsNullOrWhiteSpace(x))
                    .WithMessage("Hồ sơ cá nhân không được có tên công ty");
            });

            When(x => x.ProfileType == ProfileTypeEnum.Business, () =>
            {
                RuleFor(x => x.CompanyName)
                    .NotEmpty()
                    .WithMessage("Tên công ty là bắt buộc đối với hồ sơ doanh nghiệp")
                    .MaximumLength(255)
                    .WithMessage("Tên công ty không được vượt quá 255 ký tự");
            });

            RuleFor(x => x.Bio)
                .MaximumLength(1000)
                .WithMessage("Tiểu sử không được vượt quá 1000 ký tự")
                .When(x => !string.IsNullOrWhiteSpace(x.Bio));

            RuleFor(x => x.AvatarUrl)
                .MaximumLength(500)
                .WithMessage("URL ảnh đại diện không được vượt quá 500 ký tự")
                .When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl));

            RuleFor(x => x)
                .Must(x => x.AvatarFile == null || string.IsNullOrWhiteSpace(x.AvatarUrl))
                .WithMessage("Không thể cung cấp cả file ảnh và URL ảnh cùng lúc")
                .WithName("Avatar");
        }
    }
}