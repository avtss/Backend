using FluentValidation;
using Models.Dto.V1.Requests;

public class V1QueryAuditLogOrderRequestValidator : AbstractValidator<V1QueryAuditLogOrderRequest>
{
    public V1QueryAuditLogOrderRequestValidator()
    {
        RuleForEach(x => x.Ids)
            .GreaterThan(0)
            .WithMessage("Ids must be greater than 0");

        RuleForEach(x => x.OrderIds)
            .GreaterThan(0)
            .WithMessage("OrderIds must be greater than 0");

        RuleForEach(x => x.OrderItemIds)
            .GreaterThan(0)
            .WithMessage("OrderItemIds must be greater than 0");

        RuleForEach(x => x.CustomerIds)
            .GreaterThan(0)
            .WithMessage("CustomerIds must be greater than 0");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .When(x => x.Page > 0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .When(x => x.PageSize > 0);

        RuleFor(x => x)
            .Must(x =>
                (x.Ids != null && x.Ids.Length > 0) ||
                (x.OrderIds != null && x.OrderIds.Length > 0) ||
                (x.OrderItemIds != null && x.OrderItemIds.Length > 0) ||
                (x.CustomerIds != null && x.CustomerIds.Length > 0) ||
                (x.OrderStatuses != null && x.OrderStatuses.Length > 0) ||
                (x.Page > 0 && x.PageSize > 0))
            .WithMessage("Provide filters or paging parameters.");
    }
}
