using AISAM.Common.Dtos.Request;
using FluentValidation;

namespace AISAM.API.Validators
{
    public class UpdateAdRequestValidator : AbstractValidator<UpdateAdRequest>
    {
        private static readonly string[] Allowed = new[] { "active", "paused" };

        public UpdateAdRequestValidator()
        {
            RuleFor(x => x.Status)
                .NotEmpty()
                .Must(s => Allowed.Contains(s.ToLowerInvariant()))
                .WithMessage("Status must be one of: active, paused");
        }
    }
}


