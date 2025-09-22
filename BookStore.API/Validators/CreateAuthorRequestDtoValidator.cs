using FluentValidation;
using BookStore.API.DTO.Request;

namespace BookStore.API.Validators
{
    public class CreateAuthorRequestDtoValidator : AbstractValidator<CreateAuthorRequestDto>
    {
        public CreateAuthorRequestDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Author name is required")
                .Length(1, 200).WithMessage("Author name must be between 1 and 200 characters")
                .Matches(@"^[a-zA-Z\s\-\.\'\&]+$").WithMessage("Author name contains invalid characters");
        }
    }
}