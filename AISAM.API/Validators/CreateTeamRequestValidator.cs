using AISAM.Common.DTOs.Request;
using AISAM.Common.DTOs.Response;
using FluentValidation;

namespace AISAM.API.Validators
{
    public class CreateTeamRequestValidator : AbstractValidator<CreateTeamRequest>
    {
        public CreateTeamRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên team là bắt buộc")
                .Length(1, 255).WithMessage("Tên team phải từ 1 đến 255 ký tự")
                .Matches(@"^[a-zA-Z0-9\s\-_àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđĐ]+$")
                .WithMessage("Tên team chỉ được chứa chữ cái, số, khoảng trắng và các ký tự đặc biệt: - _");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Mô tả không được vượt quá 1000 ký tự")
                .When(x => !string.IsNullOrEmpty(x.Description));
        }
    }
}