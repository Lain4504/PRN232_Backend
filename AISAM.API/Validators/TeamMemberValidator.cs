using FluentValidation;

namespace AISAM.Common.Validators
{
    public class TeamMemberCreateValidator : AbstractValidator<TeamMemberCreateRequest>
    {
        public TeamMemberCreateValidator()
        {
            RuleFor(x => x.TeamId)
                .NotEmpty()
                .WithMessage("TeamId là bắt buộc");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("UserId là bắt buộc");

            RuleFor(x => x.Role)
                .NotEmpty()
                .WithMessage("Role là bắt buộc");
        }
    }

    public class TeamMemberUpdateValidator : AbstractValidator<TeamMemberUpdateRequest>
    {
        public TeamMemberUpdateValidator()
        {
            // Nếu TeamId có giá trị, phải là GUID hợp lệ
            RuleFor(x => x.TeamId)
                .Must(id => string.IsNullOrEmpty(id) || Guid.TryParse(id, out _))
                .WithMessage("TeamId không hợp lệ");

            // Role nếu có giá trị, không được rỗng
            RuleFor(x => x.Role)
                .Must(role => string.IsNullOrEmpty(role) || !string.IsNullOrWhiteSpace(role))
                .WithMessage("Role không hợp lệ");
        }
    }
}
