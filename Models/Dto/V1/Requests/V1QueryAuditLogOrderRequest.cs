namespace Models.Dto.V1.Requests;

public class V1QueryAuditLogOrderRequest
{
    public long[] Ids { get; set; }

    public long[] OrderIds { get; set; }

    public long[] OrderItemIds { get; set; }

    public long[] CustomerIds { get; set; }

    public string[] OrderStatuses { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }
}
