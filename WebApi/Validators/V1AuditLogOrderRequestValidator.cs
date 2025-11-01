using FluentValidation;
using Models.Dto.V1.Requests;

public class V1AuditLogOrderRequestValidator : AbstractValidator<V1AuditLogOrderRequest>
{
    public V1AuditLogOrderRequestValidator()
    {
        RuleFor(x => x.Orders)
            .NotEmpty();

        RuleForEach(x => x.Orders)
            .SetValidator(new LogOrderValidator())
            .When(x => x.Orders is not null);
    }

    private class LogOrderValidator : AbstractValidator<V1AuditLogOrderRequest.LogOrder>
    {
        public LogOrderValidator()
        {
            RuleFor(x => x.OrderId).GreaterThan(0);
            RuleFor(x => x.OrderItemId).GreaterThan(0);
            RuleFor(x => x.CustomerId).GreaterThan(0);
            RuleFor(x => x.OrderStatus).NotEmpty();
        }
    }
}
