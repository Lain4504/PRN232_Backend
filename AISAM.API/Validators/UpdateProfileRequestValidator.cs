using AISAM.Common.Dtos.Request;
using AISAM.Data.Enumeration;
using FluentValidation;

namespace AISAM.API.Validators
{
    public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
    {
        public UpdateProfileRequestValidator()
        {
            RuleFor(x => x.Name)
                .MaximumLength(255)
                .WithMessage("Tên hồ sơ không được vượt quá 255 ký tự")
                .When(x => !string.IsNullOrWhiteSpace(x.Name));

            RuleFor(x => x.ProfileType)
                .IsInEnum()
                .WithMessage("Loại hồ sơ phải là Free, Basic hoặc Pro")
                .When(x => x.ProfileType.HasValue);

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

            RuleFor(x => x.Name)
                .Must(x => x == null || !string.IsNullOrWhiteSpace(x))
                .WithMessage("Tên hồ sơ không được chỉ chứa khoảng trắng")
                .When(x => x.Name != null);

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