using FluentValidation;
using AISAM.Common.Dtos;

namespace AISAM.API.Validators
{
    public class CreateSubscriptionRequestValidator : AbstractValidator<CreateSubscriptionRequest>
    {
        public CreateSubscriptionRequestValidator()
        {
            RuleFor(x => x.Plan)
                .IsInEnum()
                .WithMessage("Invalid subscription plan");
        }
    }

    public class UpdateSubscriptionRequestValidator : AbstractValidator<UpdateSubscriptionRequest>
    {
        public UpdateSubscriptionRequestValidator()
        {
            RuleFor(x => x.Plan)
                .IsInEnum()
                .WithMessage("Invalid subscription plan");
        }
    }

    public class PaymentWebhookRequestValidator : AbstractValidator<PaymentWebhookRequest>
    {
        public PaymentWebhookRequestValidator()
        {
            RuleFor(x => x.OrderId)
                .NotEmpty()
                .WithMessage("OrderId is required");

            RuleFor(x => x.Status)
                .NotEmpty()
                .Must(status => status.ToLower() is "success" or "failed" or "refunded")
                .WithMessage("Status must be success, failed, or refunded");
        }
    }
}