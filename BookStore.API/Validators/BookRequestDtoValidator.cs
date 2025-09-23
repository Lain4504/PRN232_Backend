using BookStore.API.DTO.Request;
using FluentValidation;

namespace BookStore.API.Validators
{
    public class BookRequestValidator : AbstractValidator<BookRequestDto>
    {
        public BookRequestValidator()
        {
            RuleFor(x => x.Isbn).NotEmpty().MaximumLength(20);
            RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.Cover).MaximumLength(500);
            RuleFor(x => x.Price).GreaterThanOrEqualTo(0).When(x => x.Price.HasValue);
            RuleFor(x => x.Page).GreaterThan(0).When(x => x.Page.HasValue);
        }
    }
}
