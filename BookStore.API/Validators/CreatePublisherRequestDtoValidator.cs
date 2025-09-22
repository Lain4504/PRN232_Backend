using FluentValidation;
using BookStore.API.DTO.Request;

namespace BookStore.API.Validators
{
    public class CreatePublisherRequestDtoValidator : AbstractValidator<CreatePublisherRequestDto>
    {
        public CreatePublisherRequestDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Publisher name is required")
                .Length(1, 200).WithMessage("Publisher name must be between 1 and 200 characters")
                .Matches(@"^[a-zA-Z0-9\s\-\.\'\&]+$").WithMessage("Publisher name contains invalid characters");

            RuleFor(x => x.Website)
                .Must(uri => string.IsNullOrEmpty(uri) || Uri.TryCreate(uri, UriKind.Absolute, out _))
                .WithMessage("Website must be a valid URL")
                .When(x => !string.IsNullOrEmpty(x.Website));
        }
    }
}