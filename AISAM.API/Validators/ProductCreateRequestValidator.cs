using AISAM.Common.Dtos.Request;
using FluentValidation;

namespace AISAM.API.Validators
{
    public class ProductCreateRequestValidator : AbstractValidator<ProductCreateRequest>
    {
        public ProductCreateRequestValidator()
        {
            // BrandId bắt buộc
            RuleFor(x => x.BrandId)
                .NotEmpty().WithMessage("BrandId là bắt buộc");

            // Name bắt buộc, max 255 ký tự
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name là bắt buộc")
                .MaximumLength(255).WithMessage("Name tối đa 255 ký tự");

            // Description tối đa 2000 ký tự (nếu muốn)
            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description tối đa 2000 ký tự")
                .When(x => !string.IsNullOrWhiteSpace(x.Description));

            // Price phải >= 0 nếu có
            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0).WithMessage("Price phải >= 0")
                .When(x => x.Price.HasValue);

            // Kiểm tra ảnh upload
            RuleFor(x => x.ImageFiles)
                .NotNull().WithMessage("Cần upload ít nhất 1 ảnh")
                .Must(files => files != null && files.Any()).WithMessage("Cần upload ít nhất 1 ảnh");

            // Tuỳ chọn: kiểm tra từng file (size, content type)
            RuleForEach(x => x.ImageFiles).ChildRules(file =>
            {
                file.RuleFor(f => f.Length)
                    .LessThanOrEqualTo(5 * 1024 * 1024) // 5MB
                    .WithMessage("Ảnh không được lớn hơn 5MB");

                file.RuleFor(f => f.ContentType)
                    .Must(IsSupportedImageType)
                    .WithMessage("Chỉ hỗ trợ định dạng ảnh jpg, jpeg, png, webp");
            });
        }

        private bool IsSupportedImageType(string contentType)
        {
            return contentType == "image/jpeg" ||
                   contentType == "image/jpg" ||
                   contentType == "image/png" ||
                   contentType == "image/webp";
        }
    }
}
