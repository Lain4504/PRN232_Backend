using AISAM.Common.Models;
using AISAM.Data.Enumeration;
using FluentValidation;

namespace AISAM.API.Validators
{
    public class LinkSelectedTargetsRequestValidator : AbstractValidator<LinkSelectedTargetsRequest>
    {
        public LinkSelectedTargetsRequestValidator()
        {
            RuleFor(x => x.ProfileId)
                .NotEmpty()
                .WithMessage("ProfileId is required");

            RuleFor(x => x.Provider)
                .NotEmpty()
                .WithMessage("Provider is required");

            RuleFor(x => x.ProviderTargetIds)
                .NotEmpty()
                .WithMessage("ProviderTargetIds list cannot be empty")
                .Must(targetIds => targetIds.All(id => !string.IsNullOrWhiteSpace(id)))
                .WithMessage("All ProviderTargetIds must be non-empty strings");

            RuleFor(x => x.BrandId)
                .NotEmpty()
                .WithMessage("BrandId is required");
        }
    }
}
