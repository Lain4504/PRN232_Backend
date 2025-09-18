using AISAM.Common.Models;
using AISAM.Services.IServices;
using FluentValidation;

namespace AISAM.API.Validators
{
    public class RegisterUserDtoValidator : AbstractValidator<RegisterUserDto>
    {
        public RegisterUserDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email là bắt buộc")
                .EmailAddress().WithMessage("Email không hợp lệ");

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username là bắt buộc")
                .MinimumLength(3).WithMessage("Username phải có ít nhất 3 ký tự")
                .MaximumLength(50).WithMessage("Username không được vượt quá 50 ký tự");

            // Password validation
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches("[0-9]").WithMessage("Password must contain at least one number")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithMessage("Xác nhận mật khẩu không khớp");
        }
    }
}


