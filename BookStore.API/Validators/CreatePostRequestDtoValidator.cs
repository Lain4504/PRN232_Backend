using BookStore.API.DTO.Request;
using FluentValidation;

namespace BookStore.API.Validators
{
    public class CreatePostRequestDtoValidator : AbstractValidator<CreatePostRequestDto>
    {
        public CreatePostRequestDtoValidator()
        {
            // Title validation
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(100).WithMessage("Title cannot exceed 100 characters");

            // Content validation
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Content is required")
                .MinimumLength(10).WithMessage("Content must be at least 10 characters long");
        }
    }
}