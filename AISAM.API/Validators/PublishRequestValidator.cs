using AISAM.API.Controllers;
using AISAM.Data.Enumeration;
using AISAM.Repositories.IRepositories;
using FluentValidation;

namespace AISAM.API.Validators
{
    public class PublishRequestValidator : AbstractValidator<PostsController.PublishRequest>
    {
        private readonly IContentRepository _contentRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly ISocialIntegrationRepository _integrationRepository;

        public PublishRequestValidator(
            IContentRepository contentRepository,
            IBrandRepository brandRepository,
            ISocialIntegrationRepository integrationRepository)
        {
            _contentRepository = contentRepository;
            _brandRepository = brandRepository;
            _integrationRepository = integrationRepository;

            RuleFor(x => x.IntegrationIds)
                .NotEmpty()
                .WithMessage("Ít nhất một IntegrationId là bắt buộc");

            RuleForEach(x => x.IntegrationIds)
                .NotEmpty()
                .WithMessage("IntegrationId không được để trống");
        }

        public async Task<FluentValidation.Results.ValidationResult> ValidateAsync(
            PostsController.PublishRequest request,
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
                result.Errors.Add(new FluentValidation.Results.ValidationFailure("Content", "Nội dung phải được phê duyệt trước khi xuất bản"));
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
                result.Errors.Add(new FluentValidation.Results.ValidationFailure("Authorization", "Bạn không có quyền xuất bản cho thương hiệu này"));
            }

            // Validate each integration
            foreach (var integrationId in request.IntegrationIds)
            {
                var integration = await _integrationRepository.GetByIdAsync(integrationId);
                if (integration == null)
                {
                    result.Errors.Add(new FluentValidation.Results.ValidationFailure("IntegrationIds", $"Integration {integrationId} không tồn tại"));
                    continue;
                }

                if (!integration.IsActive)
                {
                    result.Errors.Add(new FluentValidation.Results.ValidationFailure("IntegrationIds", $"Integration {integrationId} không hoạt động"));
                }

                if (integration.BrandId != brand.Id)
                {
                    result.Errors.Add(new FluentValidation.Results.ValidationFailure("IntegrationIds", $"Integration {integrationId} không thuộc về thương hiệu này"));
                }
            }

            return result;
        }
    }
}