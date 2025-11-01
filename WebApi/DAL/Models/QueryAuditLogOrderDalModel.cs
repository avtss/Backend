namespace WebApi.DAL.Models;

public class QueryAuditLogOrderDalModel
{
    public long[] Ids { get; set; }

    public long[] OrderIds { get; set; }

    public long[] OrderItemIds { get; set; }

    public long[] CustomerIds { get; set; }

    public string[] OrderStatuses { get; set; }

    public int Limit { get; set; }

    public int Offset { get; set; }
}
