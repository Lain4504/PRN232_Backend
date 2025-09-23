using BookStore.API.DTO.Request;
using FluentValidation;

namespace BookStore.API.Validators
{
    public class CollectionRequestValidator : AbstractValidator<CollectionRequestDto>
    {
        public CollectionRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
            RuleFor(x => x.Type).MaximumLength(100);
        }
    }
}
