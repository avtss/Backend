using FluentValidation;
using Models.Dto.V1.Requests;

public class V1UpdateOrdersStatusRequestValidator : AbstractValidator<V1UpdateOrdersStatusRequest>
{
    public V1UpdateOrdersStatusRequestValidator()
    {
        RuleFor(x => x.OrderIds)
            .NotEmpty();

        RuleForEach(x => x.OrderIds)
            .GreaterThan(0);

        RuleFor(x => x.NewStatus)
            .NotEmpty();
    }
}
