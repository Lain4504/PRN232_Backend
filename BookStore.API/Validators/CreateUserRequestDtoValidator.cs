using FluentValidation;
using BookStore.API.DTO.Request;

namespace BookStore.API.Validators
{
    public class CreateUserRequestDtoValidator : AbstractValidator<CreateUserRequestDto>
    {
        public CreateUserRequestDtoValidator()
        {
            // Username validation
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required")
                .Length(3, 50).WithMessage("Username must be between 3 and 50 characters")
                .Matches("^[a-zA-Z0-9_]+$").WithMessage("Username can only contain letters, numbers, and underscores");

            // Email validation
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(256).WithMessage("Email cannot exceed 256 characters");

            // Password validation
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches("[0-9]").WithMessage("Password must contain at least one number")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");

            // FullName validation
            RuleFor(x => x.FullName)
                .MaximumLength(150).WithMessage("Full name cannot exceed 150 characters")
                .Matches("^[a-zA-Z\\s]+$").WithMessage("Full name can only contain letters and spaces")
                .When(x => !string.IsNullOrEmpty(x.FullName));

            // PhoneNumber validation
            RuleFor(x => x.PhoneNumber)
                .Matches("^[0-9+\\-\\s()]+$").WithMessage("Invalid phone number format")
                .Length(10, 15).WithMessage("Phone number must be between 10 and 15 characters")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            // Address validation
            RuleFor(x => x.Address)
                .MaximumLength(500).WithMessage("Address cannot exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.Address));

            // Role validation
            RuleFor(x => x.Role)
                .IsInEnum().WithMessage("Invalid role value");
        }
    }
}
