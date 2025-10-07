using AISAM.Common.Dtos.Request;
using FluentValidation;

namespace AISAM.Common.Validators
{
    public class TeamMemberCreateValidator : AbstractValidator<TeamMemberCreateRequest>
    {
        public TeamMemberCreateValidator()
        {
            RuleFor(x => x.TeamId).NotEmpty().WithMessage("TeamId là bắt buộc");
            RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId là bắt buộc");
            RuleFor(x => x.Role).IsInEnum().WithMessage("Role không hợp lệ");
        }
    }

    public class TeamMemberUpdateValidator : AbstractValidator<TeamMemberUpdateRequest>
    {
        public TeamMemberUpdateValidator()
        {
            RuleFor(x => x.Role)
                .IsInEnum()
                .When(x => x.Role.HasValue)
                .WithMessage("Role không hợp lệ");

            RuleFor(x => x.TeamId)
                .NotEmpty()
                .When(x => x.TeamId.HasValue)
                .WithMessage("TeamId không hợp lệ");
        }
    }
}
