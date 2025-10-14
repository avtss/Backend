using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using WebApi.DAL;
using WebApi.DAL.Interfaces;
using WebApi.DAL.Models;

namespace WebApi.DAL.Repositories;

public class AuditLogOrderRepository(UnitOfWork unitOfWork) : IAuditLogOrderRepository
{
    public async Task<V1AuditLogOrderDal[]> BulkInsert(V1AuditLogOrderDal[] models, CancellationToken token)
    {
        var sql = @"
            insert into audit_log_order
            (
                order_id,
                order_item_id,
                customer_id,
                order_status,
                created_at,
                updated_at
            )
            select
                order_id,
                order_item_id,
                customer_id,
                order_status,
                created_at,
                updated_at
            from unnest(@Entries)
            returning
                id,
                order_id,
                order_item_id,
                customer_id,
                order_status,
                created_at,
                updated_at;
        ";

        var conn = await unitOfWork.GetConnection(token);
        var res = await conn.QueryAsync<V1AuditLogOrderDal>(new CommandDefinition(
            sql, new { Entries = models }, cancellationToken: token));

        return res.ToArray();
    }

    public async Task<V1AuditLogOrderDal[]> Query(QueryAuditLogOrderDalModel model, CancellationToken token)
    {
        var sql = new StringBuilder(@"
            select
                id,
                order_id,
                order_item_id,
                customer_id,
                order_status,
                created_at,
                updated_at
            from audit_log_order
        ");

        var param = new DynamicParameters();
        var conditions = new List<string>();

        if (model.Ids?.Length > 0)
        {
            param.Add("Ids", model.Ids);
            conditions.Add("id = ANY(@Ids)");
        }

        if (model.OrderIds?.Length > 0)
        {
            param.Add("OrderIds", model.OrderIds);
            conditions.Add("order_id = ANY(@OrderIds)");
        }

        if (model.OrderItemIds?.Length > 0)
        {
            param.Add("OrderItemIds", model.OrderItemIds);
            conditions.Add("order_item_id = ANY(@OrderItemIds)");
        }

        if (model.CustomerIds?.Length > 0)
        {
            param.Add("CustomerIds", model.CustomerIds);
            conditions.Add("customer_id = ANY(@CustomerIds)");
        }

        if (model.OrderStatuses?.Length > 0)
        {
            param.Add("OrderStatuses", model.OrderStatuses);
            conditions.Add("order_status = ANY(@OrderStatuses)");
        }

        if (conditions.Count > 0)
        {
            sql.Append(" where " + string.Join(" and ", conditions));
        }

        if (model.Limit > 0)
        {
            sql.Append(" limit @Limit");
            param.Add("Limit", model.Limit);
        }

        if (model.Offset > 0)
        {
            sql.Append(" offset @Offset");
            param.Add("Offset", model.Offset);
        }

        var conn = await unitOfWork.GetConnection(token);
        var res = await conn.QueryAsync<V1AuditLogOrderDal>(new CommandDefinition(
            sql.ToString(), param, cancellationToken: token));

        return res.ToArray();
    }
}

