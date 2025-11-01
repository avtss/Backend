namespace WebApi.BLL.Models;

public class QueryAuditLogOrderModel
{
    public long[] Ids { get; set; }

    public long[] OrderIds { get; set; }

    public long[] OrderItemIds { get; set; }

    public long[] CustomerIds { get; set; }

    public string[] OrderStatuses { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }
}
