using BookStore.Common.Models;
using BookStore.Services.IServices;
using FluentValidation;

namespace BookStore.API.Validators
{
    public class RegisterUserDtoValidator : AbstractValidator<RegisterUserDto>
    {
        public RegisterUserDtoValidator(IUserService userService)
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email là bắt buộc")
                .EmailAddress().WithMessage("Email không hợp lệ")
                .MustAsync(async (email, ct) => !await userService.EmailExistsAsync(email))
                .WithMessage("Email đã được sử dụng");

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username là bắt buộc")
                .MinimumLength(3).WithMessage("Username phải có ít nhất 3 ký tự")
                .MaximumLength(50).WithMessage("Username không được vượt quá 50 ký tự")
                .MustAsync(async (username, ct) => !await userService.UsernameExistsAsync(username))
                .WithMessage("Username đã được sử dụng");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Mật khẩu là bắt buộc")
                .MinimumLength(6).WithMessage("Mật khẩu phải có ít nhất 6 ký tự");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithMessage("Xác nhận mật khẩu không khớp");
        }
    }
}


