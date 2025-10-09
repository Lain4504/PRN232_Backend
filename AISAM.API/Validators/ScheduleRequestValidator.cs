using AISAM.API.Controllers;
using AISAM.Data.Enumeration;
using AISAM.Repositories.IRepositories;
using FluentValidation;

namespace AISAM.API.Validators
{
    public class ScheduleRequestValidator : AbstractValidator<PostsController.ScheduleRequest>
    {
        private readonly IContentRepository _contentRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly ISocialIntegrationRepository _integrationRepository;

        public ScheduleRequestValidator(
            IContentRepository contentRepository,
            IBrandRepository brandRepository,
            ISocialIntegrationRepository integrationRepository)
        {
            _contentRepository = contentRepository;
            _brandRepository = brandRepository;
            _integrationRepository = integrationRepository;

            RuleFor(x => x.IntegrationId)
                .NotEmpty()
                .WithMessage("IntegrationId là bắt buộc");

            RuleFor(x => x.ScheduledAtUtc)
                .Must(BeInTheFuture)
                .WithMessage("Thời gian lên lịch phải ở tương lai");
        }

        public async Task<FluentValidation.Results.ValidationResult> ValidateAsync(
            PostsController.ScheduleRequest request,
            Guid contentId,
            Guid userId)
        {
            var result = await ValidateAsync(request);

            // Validate content exists and is approved
            var content = await _contentRepository.GetByIdAsync(contentId);
            if (content == null)
            {
                result.Errors.Add(new FluentValidation.Results.ValidationFailure("ContentId", "Nội dung không tồn tại"));
                return result;
            }

            if (content.Status != ContentStatusEnum.Approved)
            {
                result.Errors.Add(new FluentValidation.Results.ValidationFailure("Content", "Nội dung phải được phê duyệt trước khi lên lịch"));
            }

            // Validate brand ownership
            var brand = await _brandRepository.GetByIdAsync(content.BrandId);
            if (brand == null)
            {
                result.Errors.Add(new FluentValidation.Results.ValidationFailure("Brand", "Thương hiệu không tồn tại"));
                return result;
            }

            if (brand.UserId != userId)
            {
                result.Errors.Add(new FluentValidation.Results.ValidationFailure("Authorization", "Bạn không có quyền"));
            }

            // Validate integration
            var integration = await _integrationRepository.GetByIdAsync(request.IntegrationId);
            if (integration == null)
            {
                result.Errors.Add(new FluentValidation.Results.ValidationFailure("IntegrationId", "Integration không tồn tại"));
                return result;
            }

            if (!integration.IsActive)
            {
                result.Errors.Add(new FluentValidation.Results.ValidationFailure("Integration", "Integration không hoạt động"));
            }

            if (integration.BrandId != brand.Id)
            {
                result.Errors.Add(new FluentValidation.Results.ValidationFailure("Integration", "Integration không thuộc về thương hiệu này"));
            }

            return result;
        }

        private bool BeInTheFuture(DateTime scheduledAt)
        {
            return scheduledAt > DateTime.UtcNow;
        }
    }
}