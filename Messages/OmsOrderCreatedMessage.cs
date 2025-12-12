namespace Messages;

public class OmsOrderCreatedMessage : BaseMessage
{
    public override string RoutingKey => "order.created";

    public long Id { get; set; }

    public long CustomerId { get; set; }

    public string DeliveryAddress { get; set; } = string.Empty;

    public long TotalPriceCents { get; set; }

    public string TotalPriceCurrency { get; set; } = string.Empty;

    public string OrderStatus { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public OrderItemMessage[] OrderItems { get; set; } = Array.Empty<OrderItemMessage>();
}
