using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebApi.BLL.Models;
using WebApi.DAL.Interfaces;
using WebApi.DAL.Models;

namespace WebApi.BLL.Services;

public class AuditLogOrderService(IAuditLogOrderRepository repository)
{
    public async Task<AuditLogOrderUnit[]> LogAsync(AuditLogOrderUnit[] entries, CancellationToken token)
    {
        var now = DateTimeOffset.UtcNow;

        var dal = entries.Select(x => new V1AuditLogOrderDal
        {
            OrderId = x.OrderId,
            OrderItemId = x.OrderItemId,
            CustomerId = x.CustomerId,
            OrderStatus = x.OrderStatus,
            CreatedAt = now,
            UpdatedAt = now
        }).ToArray();

        var inserted = await repository.BulkInsert(dal, token);

        return Map(inserted);
    }

    public async Task<AuditLogOrderUnit[]> QueryAsync(QueryAuditLogOrderModel model, CancellationToken token)
    {
        var dal = await repository.Query(new QueryAuditLogOrderDalModel
        {
            Ids = model.Ids,
            OrderIds = model.OrderIds,
            OrderItemIds = model.OrderItemIds,
            CustomerIds = model.CustomerIds,
            OrderStatuses = model.OrderStatuses,
            Limit = model.PageSize,
            Offset = model.PageSize > 0 ? (model.Page - 1) * model.PageSize : 0
        }, token);

        return Map(dal);
    }

    private AuditLogOrderUnit[] Map(V1AuditLogOrderDal[] items)
    {
        return items.Select(x => new AuditLogOrderUnit
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
