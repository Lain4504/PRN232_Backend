using AISAM.Common.Dtos.Request;
using AISAM.Data.Enumeration;
using FluentValidation;

namespace AISAM.API.Validators
{
    public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
    {
        public UpdateProfileRequestValidator()
        {
            RuleFor(x => x.ProfileType)
                .IsInEnum()
                .WithMessage("Loại hồ sơ phải là Cá nhân hoặc Doanh nghiệp")
                .When(x => x.ProfileType.HasValue);

            RuleFor(x => x.CompanyName)
                .Must(x => !string.IsNullOrWhiteSpace(x?.Trim()))
                .WithMessage("Tên công ty là bắt buộc đối với hồ sơ doanh nghiệp")
                .When(x => x.ProfileType == ProfileTypeEnum.Business);

            RuleFor(x => x.CompanyName)
                .Must(x => string.IsNullOrWhiteSpace(x?.Trim()))
                .WithMessage("Hồ sơ cá nhân không được có tên công ty")
                .When(x => x.ProfileType.HasValue && x.ProfileType == ProfileTypeEnum.Personal);

            RuleFor(x => x.CompanyName)
                .Must(x => string.IsNullOrWhiteSpace(x) || x.Trim().Length <= 255)
                .WithMessage("Tên công ty không được vượt quá 255 ký tự")
                .When(x => !string.IsNullOrWhiteSpace(x.CompanyName));

            RuleFor(x => x.Bio)
                .Must(x => string.IsNullOrWhiteSpace(x) || x.Trim().Length <= 1000)
                .WithMessage("Tiểu sử không được vượt quá 1000 ký tự")
                .When(x => !string.IsNullOrWhiteSpace(x.Bio));

            RuleFor(x => x.AvatarUrl)
                .Must(x => string.IsNullOrWhiteSpace(x) || x.Trim().Length <= 500)
                .WithMessage("URL ảnh đại diện không được vượt quá 500 ký tự")
                .When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl));

            RuleFor(x => x)
                .Must(x => x.AvatarFile == null || string.IsNullOrWhiteSpace(x.AvatarUrl))
                .WithMessage("Không thể cung cấp cả file ảnh và URL ảnh cùng lúc")
                .WithName("Avatar");

            RuleFor(x => x.CompanyName)
                .Must(x => x == null || !string.IsNullOrWhiteSpace(x))
                .WithMessage("Tên công ty không được chỉ chứa khoảng trắng")
                .When(x => x.CompanyName != null);

            RuleFor(x => x.Bio)
                .Must(x => x == null || !string.IsNullOrWhiteSpace(x))
                .WithMessage("Tiểu sử không được chỉ chứa khoảng trắng")
                .When(x => x.Bio != null);

            RuleFor(x => x.AvatarUrl)
                .Must(x => x == null || !string.IsNullOrWhiteSpace(x))
                .WithMessage("URL ảnh đại diện không được chỉ chứa khoảng trắng")
                .When(x => x.AvatarUrl != null);
        }
    }
}