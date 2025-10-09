using AISAM.Common.Dtos.Request;
using FluentValidation;

namespace AISAM.API.Validators
{
    public class GetPostsByDateRangeRequestValidator : AbstractValidator<GetPostsByDateRangeRequest>
    {
        public GetPostsByDateRangeRequestValidator()
        {
            RuleFor(x => x.StartDate)
                .NotEmpty()
                .WithMessage("StartDate là bắt buộc")
                .LessThanOrEqualTo(x => x.EndDate)
                .WithMessage("StartDate phải nhỏ hơn hoặc bằng EndDate");

            RuleFor(x => x.EndDate)
                .NotEmpty()
                .WithMessage("EndDate là bắt buộc")
                .GreaterThanOrEqualTo(x => x.StartDate)
                .WithMessage("EndDate phải lớn hơn hoặc bằng StartDate");

            RuleFor(x => x.EndDate)
                .Must((request, endDate) => (endDate - request.StartDate).TotalDays <= 365)
                .WithMessage("Khoảng thời gian không được vượt quá 365 ngày");
        }
    }
}