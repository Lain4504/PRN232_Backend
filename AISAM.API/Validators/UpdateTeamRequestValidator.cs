using AISAM.Common.Dtos.Request;
using FluentValidation;

namespace AISAM.API.Validators
{
    public class UpdateTeamRequestValidator : AbstractValidator<UpdateTeamRequest>
    {
        public UpdateTeamRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("name là bắt buộc")
                .MaximumLength(255).WithMessage("name tối đa 255 ký tự");
            // description optional and can be empty
        }
    }
}
