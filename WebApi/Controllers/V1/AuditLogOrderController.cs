using Microsoft.AspNetCore.Mvc;
using Models.Dto.Common;
using Models.Dto.V1.Requests;
using Models.Dto.V1.Responses;
using FluentValidation;
using WebApi.BLL.Models;
using WebApi.BLL.Services;

[Route("api/v1/audit")]
public class AuditLogOrderController(
    AuditLogOrderService auditLogOrderService,
    IValidatorFactory validatorFactory
) : ControllerBase
{
    [HttpPost("log-order")]
    public async Task<ActionResult<V1AuditLogOrderResponse>> Log(
        [FromBody] V1AuditLogOrderRequest request,
        CancellationToken token)
    {
        var validationResult = await validatorFactory.GetValidator<V1AuditLogOrderRequest>()
            .ValidateAsync(request, token);

        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        var units = request.Orders.Select(x => new AuditLogOrderUnit
        {
            OrderId = x.OrderId,
            OrderItemId = x.OrderItemId,
            CustomerId = x.CustomerId,
            OrderStatus = x.OrderStatus
        }).ToArray();

        var result = await auditLogOrderService.LogAsync(units, token);

        return Ok(new V1AuditLogOrderResponse
        {
            Orders = Map(result)
        });
    }

    [HttpPost("query")]
    public async Task<ActionResult<V1AuditLogOrderResponse>> Query(
        [FromBody] V1QueryAuditLogOrderRequest request,
        CancellationToken token)
    {
        var validationResult = await validatorFactory.GetValidator<V1QueryAuditLogOrderRequest>()
            .ValidateAsync(request, token);

        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        var result = await auditLogOrderService.QueryAsync(new QueryAuditLogOrderModel
        {
            Ids = request.Ids,
            OrderIds = request.OrderIds,
            OrderItemIds = request.OrderItemIds,
            CustomerIds = request.CustomerIds,
            OrderStatuses = request.OrderStatuses,
            Page = request.Page,
            PageSize = request.PageSize
        }, token);

        return Ok(new V1AuditLogOrderResponse
        {
            Orders = Map(result)
        });
    }

    private AuditLogOrder[] Map(AuditLogOrderUnit[] units)
    {
        return units.Select(x => new AuditLogOrder
        {
            Id = x.Id,
            OrderId = x.OrderId,
            OrderItemId = x.OrderItemId,
            CustomerId = x.CustomerId,
            OrderStatus = x.OrderStatus,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        }).ToArray();
    }
}
