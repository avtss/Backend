namespace Messages;

public class OmsOrderStatusChangedMessage : BaseMessage
{
    public override string RoutingKey => "order.status.changed";

    public long OrderId { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public long[] OrderItemIds { get; set; } = Array.Empty<long>();
    public DateTimeOffset UpdatedAt { get; set; }
}
