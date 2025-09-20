using FluentValidation;
using Models.Dto.V1.Requests;

public class V1QueryOrdersRequestValidator : AbstractValidator<V1QueryOrdersRequest>
{
    public V1QueryOrdersRequestValidator()
    {
        RuleForEach(x => x.Ids)
            .GreaterThan(0)
            .WithMessage("Ids must be greater than 0");

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
                (x.CustomerIds != null && x.CustomerIds.Length > 0))
            .WithMessage("Either Ids, CustomerIds, or (Page & PageSize) must be provided");
    }
}
