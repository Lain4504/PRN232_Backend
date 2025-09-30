using AISAM.Common.Models;
using AISAM.Data.Enumeration;
using FluentValidation;
using System.Text.Json;
using AISAM.Common.Dtos.Request;

namespace AISAM.API.Validators
{
	public class CreateContentRequestValidator : AbstractValidator<CreateContentRequest>
	{
		public CreateContentRequestValidator()
		{
			RuleFor(x => x.UserId).NotEmpty();
			RuleFor(x => x.BrandId).NotEmpty();
			RuleFor(x => x.TextContent)
				.NotEmpty().WithMessage("TextContent is required")
				.MaximumLength(5000);

			RuleFor(x => x.AdType).IsInEnum();

			When(x => x.AdType == AdTypeEnum.ImageText, () =>
			{
				RuleFor(x => x.ImageUrl)
					.NotEmpty().WithMessage("image_url must be a JSON array or string for image_text")
					.Must(BeValidImageJsonOrString).WithMessage("image_url must be JSON array of non-empty strings or a single string");
			});

			When(x => x.AdType == AdTypeEnum.VideoText, () =>
			{
				RuleFor(x => x.VideoUrl)
					.NotEmpty().WithMessage("video_url is required for video_text");
			});

			When(x => x.AdType == AdTypeEnum.TextOnly, () =>
			{
				RuleFor(x => x.ImageUrl).Empty().WithMessage("image_url must be empty for text_only");
				RuleFor(x => x.VideoUrl).Empty().WithMessage("video_url must be empty for text_only");
			});

			When(x => x.PublishImmediately, () =>
			{
				RuleFor(x => x.IntegrationId)
					.NotNull().WithMessage("integrationId is required when publishImmediately is true");
			});
		}

		private bool BeValidImageJsonOrString(string? imageUrl)
		{
			if (string.IsNullOrWhiteSpace(imageUrl)) return false;

			// Accept plain string (single image)
			if (!imageUrl.TrimStart().StartsWith("["))
			{
				return true;
			}

			try
			{
				var doc = JsonDocument.Parse(imageUrl);
				if (doc.RootElement.ValueKind != JsonValueKind.Array) return false;
				foreach (var el in doc.RootElement.EnumerateArray())
				{
					if (el.ValueKind != JsonValueKind.String) return false;
					if (string.IsNullOrWhiteSpace(el.GetString())) return false;
				}
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}


